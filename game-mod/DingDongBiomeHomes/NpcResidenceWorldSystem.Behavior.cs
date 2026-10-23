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
	internal static ResidentEscortState GetState(string npcKey)
	{
		if (!States.TryGetValue(npcKey, out ResidentEscortState state))
		{
			state = new ResidentEscortState
			{
				NpcKey = npcKey,
				Stage = ResidentEscortStage.Anchored
			};
			States.Add(npcKey, state);
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
			TryEnsureOrigin(state);
		return state;
	}

	internal static void MarkBefriended(string npcKey, string ownerId, int ownerPlayerIndex)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || ownerId.Length == 0)
			return;
		ResidentEscortState state = GetState(npcKey);
		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome or ResidentEscortStage.Arrived)
			return;
		state.Stage = ResidentEscortStage.Befriended;
		state.OwnerId = ownerId;
		state.OwnerPlayerIndex = ownerPlayerIndex;
		Touch(state);
		BroadcastSnapshot();
	}

	internal static void TryClaimFlag(Player player, Point16 flagPosition)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || player is null || !player.active || player.dead)
		{
			return;
		}

		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (!questPlayer.IdentityConfirmed || questPlayer.PersistentId.Length == 0)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.IdentityPending");
			return;
		}

		if (!ResidenceFlagTileEntity.TryGet(flagPosition, out ResidenceFlagTileEntity flag))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.Missing");
			return;
		}

		Vector2 flagCenter = new Vector2(flagPosition.X * 16f + 8f, flagPosition.Y * 16f + 24f);
		if (Vector2.DistanceSquared(player.Center, flagCenter) > 12f * 16f * 12f * 16f)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.TooFar");
			return;
		}

		if (flag.Assigned)
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.AlreadyAssigned");
			return;
		}

		if (flag.Claimed && !string.Equals(flag.OwnerId, questPlayer.PersistentId, StringComparison.Ordinal))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.OwnedByAnother");
			return;
		}

		ResidentEscortState awaiting = States.Values
			.Where(state => state.Stage == ResidentEscortStage.AwaitingHome && string.Equals(state.OwnerId, questPlayer.PersistentId, StringComparison.Ordinal))
			.OrderByDescending(state => state.Revision)
			.FirstOrDefault();
		if (awaiting is null || !NpcResidentRegistry.TryGetType(awaiting.NpcKey, out int npcType))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.NoResidentReady");
			return;
		}

		if (!WithinSettlementRadius(flagPosition, awaiting.SpawnAnchor))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.OutsideSpawnRadius", SettlementRadiusTiles);
			return;
		}

		if (!AuthoredSettlementBuilder.TryValidateHousingAt(new Point(flagPosition.X, flagPosition.Y + 1), npcType, out HousingRoomInfo room, out _))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.InvalidHouse");
			return;
		}

		if (!IsRoomVacant(room.Bounds, exceptNpcType: NPCID.None, exceptNpcKey: string.Empty))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.HouseOccupied");
			return;
		}
		if (!WithinSettlementRadius(new Point16(room.HomePoint.X, room.HomePoint.Y), awaiting.SpawnAnchor))
		{
			NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.OutsideSpawnRadius", SettlementRadiusTiles);
			return;
		}

		flag.OwnerId = questPlayer.PersistentId;
		flag.NpcKey = awaiting.NpcKey;
		flag.HomePoint = room.HomePoint;
		flag.ClaimRevision = ++worldRevision;
		awaiting.FlagPosition = flagPosition;
		awaiting.DestinationHome = room.HomePoint;
		awaiting.Stage = ResidentEscortStage.Following;
		awaiting.FailureReason = string.Empty;
		Touch(awaiting);
		SyncFlag(flag);
		NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.ResidenceFlag.HomeSelected");
		BroadcastSnapshot();
	}
}
