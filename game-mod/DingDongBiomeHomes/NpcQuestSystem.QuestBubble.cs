using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
	internal static bool TryGetQuestBubbleText(NPC npc, Player player, out string text)
	{
		text = string.Empty;
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity))
		{
			return false;
		}

		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		int required = ResidentTrustPolicy.RequiredQuests(identity);
		bool hasResidence = SettlementWorldState.TryGetResidence(npcKey, out _);
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome)
			return false;
		if (!progress.Active || !hasResidence && UsesResidenceMap(progress.Definition(identity).Kind))
		{
			text = progress.Completed < required ? Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAccept", progress.Completed + 1, required) : Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAcceptContinued", progress.Completed + 1);
			return true;
		}

		ResidentQuestDefinition definition = progress.Definition(identity);
		if (progress.Paused) { text = Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Bubble.Resume"); return true; }
		if (!progress.IsAssignedTo(questPlayer.PersistentId)) { text = Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Bubble.Busy", progress.AssignedName); return true; }
		if (definition.StoryKey.Length > 0) { text = StoryBubbleText(player, definition, progress); return true; }
		text = definition.Kind switch
		{
			ResidentQuestKind.SurveyOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.SurveyOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonSurvey", SurveyTargetLabel(definition.Variant)),
			ResidentQuestKind.PatrolOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.PatrolOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonPatrol", progress.Progress, definition.Goal, PatrolDirectionLabel(PatrolPointIndex(definition.Variant, progress.Progress))),
			ResidentQuestKind.DeliverSupplies => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonSupplies", CountInventory(player, definition.ItemType), definition.Goal),
			ResidentQuestKind.DefendOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.DefendOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonDefense", progress.Progress, definition.Goal),
			ResidentQuestKind.BossHunt when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BossHunt => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBoss", definition.TargetName),
			ResidentQuestKind.BiomePurge when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BiomePurge => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBiome", progress.Progress, definition.Goal),
			ResidentQuestKind.BannerCollection when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BannerCollection => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBanner", Lang.GetItemNameValue(definition.ItemType)),
			ResidentQuestKind.ModProgression when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.ModProgression => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonProgression", definition.TargetName),
			_ => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAcceptContinued", progress.Completed + 1)};
		return true;
	}

	// Authored First Word Was forms: turn-in when ready, the pending choice, else a compact count.
	private static string StoryBubbleText(Player player, ResidentQuestDefinition definition, ResidentQuestProgress progress)
	{
		if (IsReady(player, definition, progress)) return Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn");
		if (definition.Kind == ResidentQuestKind.DialogueChoice) return Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Bubble.Choose");
		(int current, int goal) = DisplayProgress(player, definition, progress);
		string key = definition.Kind == ResidentQuestKind.Delayed ? "Waiting" : "Progress";
		return Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Bubble." + key, current, goal);
	}

	// Progress in the unit the player reads: seconds, days or items/creatures/tiles.
	internal static (int Current, int Goal) DisplayProgress(Player viewer, ResidentQuestDefinition definition, ResidentQuestProgress progress)
	{
		int current = definition.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition or ResidentQuestKind.BannerCollection
			? QuestInventoryCount(viewer, definition, progress) : progress.Progress;
		current = Math.Clamp(current, 0, definition.Goal);
		return definition.Kind switch
		{
			ResidentQuestKind.Observe or ResidentQuestKind.VisitBiome when definition.StoryKey.Length > 0 => (current / 60, Math.Max(1, definition.Goal / 60)),
			ResidentQuestKind.Delayed => (current / PersonalResidentQuestCatalog.TicksPerDay, Math.Max(1, definition.Goal / PersonalResidentQuestCatalog.TicksPerDay)),
			_ => (current, definition.Goal)
		};
	}
}
