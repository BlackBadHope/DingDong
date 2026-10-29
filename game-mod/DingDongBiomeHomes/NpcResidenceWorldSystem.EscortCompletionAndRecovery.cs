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
	private const string InvalidDestinationReason = "destination-invalid-or-occupied";

	internal static bool IsWaitingForDestination(ResidentEscortState state) =>
		state.Stage == ResidentEscortStage.Waiting && string.Equals(state.FailureReason, InvalidDestinationReason, StringComparison.Ordinal);

	internal static bool TryCompleteEscort(NPC npc, string npcKey, Player owner)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || owner is null || !owner.active || owner.dead || npc is null || !npc.active)
			return false;
		ResidentEscortState state = GetState(npcKey);
		if (state.Stage is not (ResidentEscortStage.Following or ResidentEscortStage.Waiting) || !string.Equals(state.OwnerId, owner.GetModPlayer<NpcQuestPlayer>().PersistentId, StringComparison.Ordinal))
		{
			return false;
		}

		if (!HasAssignedDestination(state))
		{
			Vector2 spawn = state.SpawnAnchor.ToWorldCoordinates(8f, 8f);
			float npcRadius = SpawnArrivalRadiusTiles * 16f;
			float ownerRadius = (SpawnArrivalRadiusTiles + 8) * 16f;
			if (Vector2.DistanceSquared(npc.Center, spawn) > npcRadius * npcRadius || Vector2.DistanceSquared(owner.Center, spawn) > ownerRadius * ownerRadius)
				return false;
			state.Stage = ResidentEscortStage.AwaitingHome;
			state.FailureReason = string.Empty;
			Touch(state);
			NpcQuestSystem.Notify(owner, "Mods.DingDongBiomeHomes.Quests.SpawnReached", SettlementRadiusTiles);
			BroadcastSnapshot();
			return true;
		}

		if (!ResidenceFlagTileEntity.TryGet(state.FlagPosition, out ResidenceFlagTileEntity flag))
			return false;

		Vector2 destination = state.DestinationHome.ToWorldCoordinates(8f, 8f);
		if (!IsWaitingForDestination(state) &&
			(Vector2.DistanceSquared(npc.Center, destination) > 8f * 16f * 8f * 16f || Vector2.DistanceSquared(owner.Center, destination) > 12f * 16f * 12f * 16f))
		{
			return false;
		}

		if (flag.OwnerId != state.OwnerId || flag.NpcKey != npcKey ||
			!WithinSettlementRadius(flag.Position, state.SpawnAnchor) ||
			!AuthoredSettlementBuilder.TryValidateHousingAt(new Point(flag.Position.X, flag.Position.Y + 1), npc.type, out HousingRoomInfo room, out _) ||
			!WithinSettlementRadius(new Point16(room.HomePoint.X, room.HomePoint.Y), state.SpawnAnchor) ||
			!IsRoomVacant(room.Bounds, npc.type, npcKey))
		{
			bool alreadyWaiting = IsWaitingForDestination(state);
			SetWaiting(npcKey, InvalidDestinationReason);
			if (!alreadyWaiting)
				NpcQuestSystem.Notify(owner, "Mods.DingDongBiomeHomes.ResidenceFlag.InvalidOrOccupied");
			return false;
		}

		flag.NpcKey = npcKey;
		flag.HomePoint = room.HomePoint;
		state.DestinationHome = room.HomePoint;
		// Rebuilding the room can move its home tile after the flag was selected.
		// The NPC must walk to the revalidated point, not arrive at the stale one.
		destination = room.HomePoint.ToWorldCoordinates(8f, 8f);
		if (Vector2.DistanceSquared(npc.Center, destination) > 128f * 128f ||
			Vector2.DistanceSquared(owner.Center, destination) > 192f * 192f)
		{
			// Validation succeeded. A moved home can require more walking, but
			// the earlier waiting reason is no longer true.
			state.Stage = ResidentEscortStage.Following;
			state.FailureReason = string.Empty;
			Touch(state);
			SyncFlag(flag);
			BroadcastSnapshot();
			return false;
		}
		state.Stage = ResidentEscortStage.Arrived;
		state.OriginReleased = true;
		state.FailureReason = string.Empty;
		Touch(state);
		ApplyHome(npc, state.DestinationHome);
		SyncFlag(flag);
		npc.netUpdate = true;
		NpcQuestSystem.Notify(owner, "Mods.DingDongBiomeHomes.Quests.Arrived");
		BroadcastSnapshot();
		return true;
	}

	internal static void SetWaiting(string npcKey, string reason)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		ResidentEscortState state = GetState(npcKey);
		if (state.Stage is not (ResidentEscortStage.Following or ResidentEscortStage.Waiting))
			return;
		if (state.Stage == ResidentEscortStage.Waiting && string.Equals(state.FailureReason, reason, StringComparison.Ordinal))
			return;
		state.Stage = ResidentEscortStage.Waiting;
		state.FailureReason = reason;
		Touch(state);
		BroadcastSnapshot();
	}

	internal static void ResumeFollowing(string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		ResidentEscortState state = GetState(npcKey);
		if (state.Stage != ResidentEscortStage.Waiting)
			return;
		state.Stage = ResidentEscortStage.Following;
		state.FailureReason = string.Empty;
		Touch(state);
		BroadcastSnapshot();
	}

	internal static void ApplyLockedHome(NPC npc, string npcKey)
	{
        if(ResidentEncounterSystem.IsPending(npcKey))return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		ResidentEscortState state = GetState(npcKey);
		Point home = state.Stage switch
		{
			ResidentEscortStage.Arrived => state.DestinationHome,
			ResidentEscortStage.AwaitingHome => state.SpawnAnchor,
			ResidentEscortStage.Following when HasAssignedDestination(state) => state.SpawnAnchor,
			_ => state.OriginHome
		};
        if(!state.OriginReleased && state.Stage is ResidentEscortStage.Anchored or ResidentEscortStage.Befriended or ResidentEscortStage.Invited &&
            SettlementWorldState.TryGetResidence(npcKey,out var residence) && ResidentEncounterTerrain.TryHomeAnchor(residence,npc.type,out var anchor))
            home=new Point(anchor.X,anchor.Y+2);
		if (home.X > 0 && home.Y > 0)
			ApplyHome(npc, home);
	}

	private static bool HasAssignedDestination(ResidentEscortState state) =>
		state.FlagPosition.X >= 0 && state.FlagPosition.Y >= 0 && state.DestinationHome.X > 0 && state.DestinationHome.Y > 0;

	private static void BroadcastSnapshot()
	{
		if (Main.netMode == NetmodeID.Server)
			SendSnapshot(-1);
	}
}
