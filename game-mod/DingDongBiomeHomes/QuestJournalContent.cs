using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal static class QuestJournalContent
{
	internal static QuestJournalSnapshot Build(Player player)
	{
		IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> records = player.GetModPlayer<NpcQuestPlayer>().CurrentSnapshot();
		List<QuestJournalEntry> entries = new();
		int readyCount = 0;
		int completed = 0;
		int residentsKnown = 0;
		foreach ((string npcKey, ResidentQuestProgress progress)in records)
		{
			completed += progress.Completed;
			if (progress.Completed > 0 || progress.Active)
				residentsKnown++;
			if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
				continue;
			if (!progress.Active)
			{
				if (progress.Completed <= 0)
					continue;
				AddTrustStatusEntry(entries, npcKey, progress, identity);
				continue;
			}
			ResidentQuestDefinition definition = progress.Definition(identity);
			bool ready = NpcQuestSystem.IsReady(player, definition, progress);
			if (ready)
				readyCount++;
			string objective = NpcQuestSystem.ObjectiveText(player, definition, progress)+"\n"+progress.AssignedName;
			if (ready)
				objective += "\n" + Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.ReturnToResident", NpcQuestSystem.ResidentName(npcKey));
			else if (NpcQuestSystem.UsesResidenceMap(definition.Kind))
				objective += "\n" + Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.MapHint");
			int trustRequired = ResidentTrustPolicy.RequiredQuests(identity);
			string trust = Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.Trust", ResidentTrustPolicy.Percent(progress, identity), progress.Completed, trustRequired);
			string body = Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.EntryBody", objective, ready ? Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.Ready") : Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.InProgress"), QuestRewardBalance.FormatBundle(definition, progress.RewardCopper, progress.DifficultyRank), NpcQuestSystem.DifficultyName(progress.DifficultyRank), trust);
			(int iconItemType, int iconNpcType) = ResolveIcon(definition);
			entries.Add(new QuestJournalEntry(NpcQuestSystem.ResidentName(npcKey), body, ready, iconItemType, iconNpcType, definition.StoryKey.Length>0?definition.RewardItemType:QuestRewardBalance.CacheItemType(progress.DifficultyRank)));
		}

		entries = entries.OrderByDescending(entry => entry.Ready).ThenBy(entry => entry.Resident, StringComparer.CurrentCulture).ToList();
		if (entries.Count == 0)
		{
			entries.Add(new QuestJournalEntry(Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.EmptyTitle"), Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.EmptyBody"), false, ItemID.Book, NPCID.None, ItemID.None));
		}

		return new QuestJournalSnapshot(Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.Title"), Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.Subtitle"), Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.Summary", records.Count(record => record.Progress.Active), readyCount, completed, residentsKnown), entries.AsReadOnly());
	}

	private static void AddTrustStatusEntry(List<QuestJournalEntry> entries, string npcKey, ResidentQuestProgress progress, NpcTownIdentity identity)
	{
		int required = ResidentTrustPolicy.RequiredQuests(identity);
		int percent = ResidentTrustPolicy.Percent(progress, identity);
		bool unlocked = progress.Completed >= required;
		string body;
		if (!unlocked)
		{
			body = Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Journal.TrustProgress", percent, progress.Completed, required, Math.Max(0, required - progress.Completed));
		}
		else
		{
			ResidentEscortStage stage = NpcResidenceWorldSystem.GetState(npcKey).Stage;
			string key = stage switch
			{
				ResidentEscortStage.Arrived => "ResidentSettled",
				ResidentEscortStage.Following or ResidentEscortStage.Waiting => "ResidentFollowing",
				ResidentEscortStage.AwaitingHome => "ResidentAwaitingHome",
				_ => "TrustUnlocked"
			};
			body = Language.GetTextValue($"Mods.DingDongBiomeHomes.Quests.Journal.{key}", progress.Completed, required, NpcResidenceWorldSystem.SettlementRadiusTiles);
		}

		int npcType = NpcResidentRegistry.TryGetType(npcKey, out int resolvedType) ? resolvedType : NPCID.None;
		entries.Add(new QuestJournalEntry(NpcQuestSystem.ResidentName(npcKey), body, unlocked, ItemID.None, npcType, ItemID.None));
	}

	private static (int ItemType, int NpcType) ResolveIcon(ResidentQuestDefinition definition)
	{
		if (definition.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.BannerCollection && definition.ItemType > ItemID.None)
		{
			return (definition.ItemType, NPCID.None);
		}

		if (definition.Kind == ResidentQuestKind.BossHunt && QuestContentRegistry.TryGetNpcTargetType(definition.TargetKey, out int npcType))
		{
			return (ItemID.None, npcType);
		}

		if (definition.Kind == ResidentQuestKind.ModProgression && !definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) && QuestContentRegistry.TryGetNextUndefeatedBossType(definition.TargetKey, out int bossType))
		{
			return (ItemID.None, bossType);
		}

		int itemType = definition.Kind switch
		{
			ResidentQuestKind.SurveyOrigin => ItemID.Binoculars,
			ResidentQuestKind.PatrolOrigin => ItemID.Compass,
			ResidentQuestKind.DefendOrigin => ItemID.CobaltShield,
			ResidentQuestKind.BiomePurge => BiomeIcon(definition.TargetKey),
			ResidentQuestKind.ModProgression => ItemID.Book,
			_ => ItemID.Book
		};
		return (itemType, NPCID.None);
	}

	private static int BiomeIcon(string targetKey) => targetKey switch
	{
		"Forest" => ItemID.Acorn,
		"Snow" => ItemID.IceBlock,
		"Desert" => ItemID.SandBlock,
		"Jungle" => ItemID.JungleGrassSeeds,
		"Ocean" => ItemID.Coral,
		"Underground" => ItemID.StoneBlock,
		"Hallow" => ItemID.PearlstoneBlock,
		"Mushroom" => ItemID.GlowingMushroom,
		"Underworld" => ItemID.Hellstone,
		_ => ItemID.Book
	};
}
