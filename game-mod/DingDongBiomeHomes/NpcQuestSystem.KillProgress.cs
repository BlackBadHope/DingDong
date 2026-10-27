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
	internal static void RecordNpcKill(Player player, NPC defeatedNpc)
	{
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		bool changed = false;
		string contentKey = QuestContentRegistry.ContentKey(defeatedNpc);
		string contentMod = QuestContentRegistry.ContentMod(defeatedNpc);
		bool newlyDefeatedBoss = defeatedNpc.boss && !QuestWorldProgressSystem.WasBossDefeated(contentKey);
		foreach ((string npcKey, ResidentQuestProgress progress)in questPlayer.CurrentActiveRecords())
		{
			if (!progress.IsAssignedTo(questPlayer.PersistentId)) continue;
			if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
				continue;
			ResidentQuestDefinition definition = progress.Definition(identity);
			if (progress.Progress >= definition.Goal)
				continue;
			bool matched = false;
			switch (definition.Kind)
			{
				case ResidentQuestKind.DefendOrigin:
					if (SettlementWorldState.TryGetResidence(npcKey, out SettlementResidenceReport residence))
					{
						Rectangle defenseArea = DefenseArea(residence, definition.RadiusTiles);
						matched = defenseArea.Contains(defeatedNpc.Center.ToTileCoordinates());
					}

					break;
				case ResidentQuestKind.CreatureHunt:
                        matched = string.Equals(contentKey, definition.TargetKey, StringComparison.Ordinal);
                        break;
				case ResidentQuestKind.Trial:
					// A kill counts only while the forbidden state is absent (QUEST-FORMS "Trial").
					matched = string.Equals(contentKey, definition.TargetKey, StringComparison.Ordinal) && !TrialStateViolated(player, definition);
					break;
				case ResidentQuestKind.BossHunt:
					matched = defeatedNpc.boss && string.Equals(contentKey, definition.TargetKey, StringComparison.Ordinal);
					break;
				case ResidentQuestKind.BiomePurge:
					matched = QuestContentRegistry.PlayerInBiome(player, definition.TargetKey);
					break;
				case ResidentQuestKind.ModProgression when !definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal):
					matched = newlyDefeatedBoss && string.Equals(contentMod, definition.TargetKey, StringComparison.Ordinal);
					break;
			}

			if (!matched)
				continue;
			progress.Progress = Math.Min(definition.Goal, progress.Progress + 1);
			NpcQuestWorldLedger.Record(player, npcKey, progress);
			changed = true;
			if (progress.Progress >= definition.Goal)
				Notify(player, "Mods.DingDongBiomeHomes.Quests.ObjectiveReady", ResidentName(npcKey));
		}

		if (changed)
			NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}

	internal static void Notify(Player player, string key, params object[] args)
	{
		if (Main.netMode == NetmodeID.Server && NpcQuestNetwork.CanSendToClient(player.whoAmI))
			ChatHelper.SendChatMessageToClient(NetworkText.FromKey(key, args), new Color(110, 190, 255), player.whoAmI);
		else if (player.whoAmI == Main.myPlayer)
			Main.NewText(Language.GetTextValue(key, args), 110, 190, 255);
	}
}
