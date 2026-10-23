using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class NpcEscortMobilityGlobalNPC
{
	private static void Stop(NPC npc)
	{
		npc.velocity.X *= 0.6f;
		if (Math.Abs(npc.velocity.X) < 0.08f)
			npc.velocity.X = 0f;
	}

	private static void TryOpenDoor(NPC npc, int direction)
	{
		if (direction == 0 || Main.netMode == NetmodeID.MultiplayerClient)
			return;
		int tileX = (int)((npc.Center.X + direction * (npc.width * 0.5f + 6f)) / 16f);
		int centerY = (int)(npc.Center.Y / 16f);
		for (int y = centerY - 2; y <= centerY + 1; y++)
		{
			if (!WorldGen.InWorld(tileX, y, 10))
				continue;
			Tile tile = Framing.GetTileSafely(tileX, y);
			if (!tile.HasTile || tile.TileType >= TileID.Sets.OpenDoorID.Length || TileID.Sets.OpenDoorID[tile.TileType] < 0)
				continue;
			if (WorldGen.OpenDoor(tileX, y, direction))
			{
				NetMessage.SendTileSquare(-1, tileX, y, 4);
				return;
			}
		}
	}
}
