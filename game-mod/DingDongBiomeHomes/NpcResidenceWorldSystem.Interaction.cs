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
	internal static void HandlePlayerConnected(Player player)
	{
		if (Main.netMode != NetmodeID.Server)
			return;
		string ownerId = player.GetModPlayer<NpcQuestPlayer>().PersistentId;
		bool changed = false;
		foreach (ResidentEscortState state in States.Values)
		{
			if (!string.Equals(state.OwnerId, ownerId, StringComparison.Ordinal) || state.OwnerPlayerIndex == player.whoAmI)
				continue;
			state.OwnerPlayerIndex = player.whoAmI;
			Touch(state);
			changed = true;
		}

		if (changed)
			BroadcastSnapshot();
	}

	internal static void HandleResidentDeath(string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		ResidentEscortState state = GetState(npcKey);
		ResetToOrigin(state, clearFlagAssignment: true, "resident-died");
		BroadcastSnapshot();
	}

	internal static void HandleFlagRemoved(Point16 flagPosition)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		bool changed = false;
		foreach (ResidentEscortState state in States.Values.Where(state => state.FlagPosition.Equals(flagPosition)).ToArray())
		{
			ResetToOrigin(state, clearFlagAssignment: false, "flag-removed");
			changed = true;
		}

		if (changed)
			BroadcastSnapshot();
	}
}
