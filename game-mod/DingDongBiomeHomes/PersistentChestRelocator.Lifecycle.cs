using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
/// <summary>
/// Lossless chest relocation for dedicated probes and authored worldgen. The
/// existing chest index is rebound to a verified destination before source
/// tiles are cleared, so full modded chest arrays remain safe.
/// </summary>
public static partial class PersistentChestRelocator
{
	private static void ClearChestTiles(Point16 topLeft)
	{
		for (int x = topLeft.X; x <= topLeft.X + 1; x++)
		{
			for (int y = topLeft.Y; y <= topLeft.Y + 1; y++)
				Main.tile[x, y].ClearTile();
		}
	}
}
