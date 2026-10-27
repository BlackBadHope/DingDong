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
	internal static void HandleInteraction(Player player, int npcIndex, bool requestEscort)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || player is null || !player.active || player.dead ||
			!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || npcIndex < 0 || npcIndex >= Main.maxNPCs)
		{
			return;
		}

		if (!player.GetModPlayer<NpcQuestPlayer>().IdentityConfirmed)
		{
			Notify(player, "Mods.DingDongBiomeHomes.Quests.IdentityPending");
			return;
		}

		NPC npc = Main.npc[npcIndex];
		if (!npc.active || Vector2.DistanceSquared(player.Center, npc.Center) > 14f * 16f * 14f * 16f || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity))
		{
			Notify(player, "Mods.DingDongBiomeHomes.Quests.InvalidResident");
			return;
		}

		if(!ResidentEncounterSystem.CanOfferQuests(npcKey)) { Notify(player,"Mods.DingDongBiomeHomes.SharedQuests.EncounterPending"); return; }
        NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		if (progress.Active && !progress.IsAssignedTo(questPlayer.PersistentId))
		{
			Notify(player, "Mods.DingDongBiomeHomes.SharedQuests.Assigned", progress.AssignedName);
			return;
		}
		if (RepairUnavailableProgression(player, npcKey, identity, progress))
		{
			NpcQuestWorldLedger.Record(player, npcKey, progress);
			NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
			return;
		}
		bool hasResidence = SettlementWorldState.TryGetResidence(npcKey, out _);
		int required = ResidentTrustPolicy.RequiredQuests(identity);
		ResidentEscortState escortState = NpcResidenceWorldSystem.GetState(npcKey);
		if (escortState.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting)
		{
			NpcResidenceWorldSystem.TryBeginEscort(player, npc, npcKey);
			return;
		}

		if (requestEscort)
		{
			if (!hasResidence)
			{
				Notify(player, "Mods.DingDongBiomeHomes.Quests.InvalidResident");
				return;
			}

			if (progress.Active)
			{
				Notify(player, "Mods.DingDongBiomeHomes.Quests.FinishBeforeEscort");
				return;
			}

			if (progress.Completed < required)
			{
				Notify(player, "Mods.DingDongBiomeHomes.Quests.TrustNeeded", progress.Completed, required);
				return;
			}

			NpcResidenceWorldSystem.TryBeginEscort(player, npc, npcKey);
			return;
		}

		if (progress.Active && !hasResidence && UsesResidenceMap(progress.Definition(identity).Kind))
		{
			// Repair an objective that became impossible after a partial/legacy
			// world load. Completed trust is retained; only the active objective is
			// replaced by a residence-independent quest.
			progress.ClearActive();
		}

		if (!progress.Active)
		{
            if (PersonalResidentQuestCatalog.Complete(npcKey, progress.Completed))
            { Notify(player, "Mods.DingDongBiomeHomes.SharedQuests.StoryComplete"); return; }
			ResidentQuestDefinition selected = ResidentQuestCatalog.SelectAvailable(questPlayer.PersistentId, identity, progress.Completed, allowResidenceObjectives: hasResidence);
			ResidentQuestDefinition scaled = QuestRewardBalance.Scale(selected, out int rewardCopper, out int difficultyRank);
			progress.Begin(scaled, rewardCopper, difficultyRank);
			progress.AssignedTo = questPlayer.PersistentId;
			progress.AssignedName = player.name.Length <= 60 ? player.name : player.name[..60];
			NpcQuestWorldLedger.Record(player, npcKey, progress);
			NotifyQuestAccepted(player, scaled, identity);
			if (UsesResidenceMap(scaled.Kind))
				Notify(player, "Mods.DingDongBiomeHomes.Quests.MapTargetMarked");
			Notify(player, "Mods.DingDongBiomeHomes.Quests.RewardPromised", QuestRewardBalance.FormatBundle(scaled, rewardCopper, difficultyRank));
			NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
			return;
		}

		ResidentQuestDefinition definition = progress.Definition(identity);
		switch (definition.Kind)
		{
			case ResidentQuestKind.SurveyOrigin:
				if (progress.Progress < definition.Goal)
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.SurveyHint", SurveyTargetLabel(definition.Variant));
					return;
				}

				break;
			case ResidentQuestKind.PatrolOrigin:
				if (progress.Progress < definition.Goal)
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.PatrolMissing", progress.Progress, definition.Goal, PatrolDirectionLabel(PatrolPointIndex(definition.Variant, progress.Progress)));
					return;
				}

				break;
			case ResidentQuestKind.DeliverSupplies:
				if (CountQuestSupplies(player, definition) < definition.Goal)
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.SuppliesMissing", Lang.GetItemNameValue(definition.ItemType), definition.Goal);
					return;
				}

				ConsumeQuestSupplies(player, definition);
				break;
			case ResidentQuestKind.DefendOrigin:
				if (progress.Progress < definition.Goal)
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.DefenseMissing", progress.Progress, definition.Goal);
					return;
				}

				break;
			case ResidentQuestKind.Expedition:
				if (CountQuestSupplies(player, definition) < definition.Goal)
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.ObjectiveMissing", ObjectiveText(player, definition, progress));
					return;
				}

				ConsumeQuestSupplies(player, definition);
				break;
			case ResidentQuestKind.CreatureHunt:
            case ResidentQuestKind.VisitBiome:
			case ResidentQuestKind.BossHunt:
			case ResidentQuestKind.BiomePurge:
			case ResidentQuestKind.BannerCollection:
			case ResidentQuestKind.ModProgression:
			case ResidentQuestKind.Observe:
			case ResidentQuestKind.GatherEnv:
			case ResidentQuestKind.Trial:
			case ResidentQuestKind.DialogueChoice:
			case ResidentQuestKind.Delayed:
				if (!IsReady(player, definition, progress))
				{
					Notify(player, "Mods.DingDongBiomeHomes.Quests.ObjectiveMissing", ObjectiveText(player, definition, progress));
					return;
				}

				if (definition.Kind == ResidentQuestKind.BannerCollection)
					ConsumeInventory(player, definition.ItemType, 1);
				break;
		}

		CompleteQuestAndGrantReward(player, npcKey, questPlayer, progress, required);
	}
}
