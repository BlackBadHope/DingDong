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
	internal static Player FindOwner(ResidentEscortState state)
	{
		if (state.OwnerPlayerIndex >= 0 && state.OwnerPlayerIndex < Main.maxPlayers)
		{
			Player indexed = Main.player[state.OwnerPlayerIndex];
			if (indexed.active && !indexed.dead && (Main.netMode == NetmodeID.MultiplayerClient || string.Equals(indexed.GetModPlayer<NpcQuestPlayer>().PersistentId, state.OwnerId, StringComparison.Ordinal)))
			{
				return indexed;
			}
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
			return null;
		for (int index = 0; index < Main.maxPlayers; index++)
		{
			Player player = Main.player[index];
			if (player.active && !player.dead && string.Equals(player.GetModPlayer<NpcQuestPlayer>().PersistentId, state.OwnerId, StringComparison.Ordinal))
			{
				if (state.OwnerPlayerIndex != index)
				{
					state.OwnerPlayerIndex = index;
					Touch(state);
					BroadcastSnapshot();
				}

				return player;
			}
		}

		return null;
	}
}
