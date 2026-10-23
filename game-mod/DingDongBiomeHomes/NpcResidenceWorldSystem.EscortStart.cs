using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class NpcResidenceWorldSystem
{
	internal static void TryBeginEscort(Player player, NPC npc, string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort ||
			player is null || !player.active || player.dead || npc is null || !npc.active)
			return;
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (!questPlayer.IdentityConfirmed || string.IsNullOrEmpty(questPlayer.PersistentId) ||
			!NpcResidentRegistry.TryGet(npc, out string actualKey, out NpcTownIdentity identity) || actualKey != npcKey ||
			!SettlementWorldState.TryGetResidence(npcKey, out _) ||
			Vector2.DistanceSquared(player.Center, npc.Center) > 14f * 16f * 14f * 16f)
			return;
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		if (progress.Active || progress.Completed < ResidentTrustPolicy.RequiredQuests(identity))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Quests.TrustNeeded", progress.Completed, ResidentTrustPolicy.RequiredQuests(identity));
			return;
		}
		ResidentEscortState state = GetState(npcKey);
		if (!TryEnsureOrigin(state)) return;
		// Legacy Befriended state is not ownership: only an explicit escort claims it.
		if (state.Stage == ResidentEscortStage.Arrived)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Quests.AlreadyMoved");
			return;
		}

		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting)
		{
			NpcQuestSystem.Notify(player, string.Equals(state.OwnerId, questPlayer.PersistentId, StringComparison.Ordinal) ? "Mods.DingDongBiomeHomes.Quests.AlreadyFollowing" : "Mods.DingDongBiomeHomes.Quests.Busy");
			return;
		}

		if (state.Stage == ResidentEscortStage.AwaitingHome)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Quests.AwaitingHome");
			return;
		}

		ResidentEscortState existing = States.Values.FirstOrDefault(candidate =>
			!string.Equals(candidate.NpcKey, npcKey, StringComparison.Ordinal) &&
			string.Equals(candidate.OwnerId, questPlayer.PersistentId, StringComparison.Ordinal) &&
			candidate.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome);
		if (existing is not null)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Quests.AnotherEscortActive");
			return;
		}

		state.Stage = ResidentEscortStage.Invited;
		state.OwnerId = questPlayer.PersistentId;
		state.OwnerPlayerIndex = player.whoAmI;
		state.FlagPosition = new Point16(-1, -1);
		state.SpawnAnchor = new Point(Main.spawnTileX, Main.spawnTileY);
		state.DestinationHome = new Point(-1, -1);
		state.FailureReason = string.Empty;
		Touch(state);
		// Invited is a persisted transition; physical following starts without relocating the NPC.
		state.Stage = ResidentEscortStage.Following;
		Touch(state);
		npc.netUpdate = true;
		NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Quests.EscortStarted");
		BroadcastSnapshot();
	}
}
