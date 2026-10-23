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
	internal static void UpdateLocationProgress(Player player)
	{
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		bool changed = false;
		foreach ((string npcKey, ResidentQuestProgress progress)in questPlayer.CurrentActiveRecords())
		{
			if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
				continue;
			if (RepairUnavailableProgression(player, npcKey, identity, progress))
			{
				NpcQuestWorldLedger.Record(player, npcKey, progress);
				changed = true;
				continue;
			}
			ResidentQuestDefinition definition = progress.Definition(identity);
			bool record = false;
			if (definition.Kind is ResidentQuestKind.SurveyOrigin or ResidentQuestKind.PatrolOrigin)
			{
				if (!SettlementWorldState.TryGetResidence(npcKey, out SettlementResidenceReport residence) || progress.Progress >= definition.Goal)
					continue;
				Point target = definition.Kind == ResidentQuestKind.SurveyOrigin ? residence.QuestPoint(false, definition.Variant) : residence.QuestPoint(true, PatrolPointIndex(definition.Variant, progress.Progress));
				if (!IsNear(player, target, definition.RadiusTiles))
					continue;
				progress.Progress = definition.Kind == ResidentQuestKind.SurveyOrigin ? definition.Goal : Math.Min(definition.Goal, progress.Progress + 1);
				record = true;
				Notify(player, progress.Progress >= definition.Goal ? definition.Kind == ResidentQuestKind.SurveyOrigin ? "Mods.DingDongBiomeHomes.Quests.SurveyReady" : "Mods.DingDongBiomeHomes.Quests.PatrolReady" : "Mods.DingDongBiomeHomes.Quests.JournalProgress", ResidentName(npcKey), progress.Progress, definition.Goal);
			}
			else if (definition.Kind == ResidentQuestKind.BannerCollection)
			{
				int inventoryProgress = CountInventory(player, definition.ItemType) > 0 ? 1 : 0;
				if (inventoryProgress != progress.Progress)
				{
					progress.Progress = inventoryProgress;
					record = true;
				}
			}
			else if (definition.Kind == ResidentQuestKind.ModProgression && definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) && progress.Progress < definition.Goal && QuestContentRegistry.IsExternalMilestoneComplete(definition.TargetKey))
			{
				progress.Progress = definition.Goal;
				record = true;
			}

			if (!record)
				continue;
			NpcQuestWorldLedger.Record(player, npcKey, progress);
			changed = true;
		}

		if (changed)
			NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}
}
