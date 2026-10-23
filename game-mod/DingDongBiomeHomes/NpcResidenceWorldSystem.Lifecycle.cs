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
	public override void ClearWorld()
	{
		States.Clear();
		OriginScans.Clear();
		worldRevision = 0;
	}

	public override void PostUpdateWorld()
	{
		ProcessOriginScans();
		if (Main.netMode == NetmodeID.MultiplayerClient || Main.gameMenu || Main.GameUpdateCount % 120UL != 0UL)
		{
			return;
		}

		bool changed = false;
		foreach (ResidentEscortState state in States.Values)
		{
			if (state.Stage is not (ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.Arrived))
				continue;
			if (!HasAssignedDestination(state))
				continue;
			if (ResidenceFlagTileEntity.TryGet(state.FlagPosition, out ResidenceFlagTileEntity flag) && string.Equals(flag.OwnerId, state.OwnerId, StringComparison.Ordinal) && (flag.NpcKey.Length == 0 || string.Equals(flag.NpcKey, state.NpcKey, StringComparison.Ordinal)))
			{
				continue;
			}

			ResetToOrigin(state, clearFlagAssignment: false, "flag-missing-or-reassigned");
			changed = true;
		}

		if (changed)
			BroadcastSnapshot();
	}

	internal static void HandlePlayerDisconnected(int playerIndex)
	{
		if (Main.netMode != NetmodeID.Server)
			return;
		bool changed = false;
		foreach (ResidentEscortState state in States.Values)
		{
			if (state.OwnerPlayerIndex != playerIndex)
				continue;
			state.OwnerPlayerIndex = -1;
			Touch(state);
			changed = true;
		}

		if (changed)
			BroadcastSnapshot();
	}

	private static void ResetToOrigin(ResidentEscortState state, bool clearFlagAssignment, string reason)
	{
		if (clearFlagAssignment && ResidenceFlagTileEntity.TryGet(state.FlagPosition, out ResidenceFlagTileEntity flag) && string.Equals(flag.NpcKey, state.NpcKey, StringComparison.Ordinal))
		{
			flag.NpcKey = string.Empty;
			SyncFlag(flag);
		}

		state.Stage = ResidentEscortStage.Anchored;
		state.OwnerId = string.Empty;
		state.OwnerPlayerIndex = -1;
		state.FlagPosition = new Point16(-1, -1);
		state.SpawnAnchor = new Point(-1, -1);
		state.DestinationHome = new Point(-1, -1);
		state.FailureReason = reason;
		Touch(state);
	}
}
