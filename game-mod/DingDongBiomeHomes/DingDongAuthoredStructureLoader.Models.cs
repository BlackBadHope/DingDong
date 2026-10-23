using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Runtime-independent reader for the reviewed Structure Helper 3.0.1/3.1.0
/// vanilla subset used by DingDong. It intentionally rejects NBT, custom
/// tile-data blocks and arbitrary modded tile/wall mappings instead of loading
/// another assembly.
/// </summary>
public static partial class DingDongAuthoredStructureLoader
{
	private sealed record StructureData(int Width, int Height, ushort? NullTileId, ushort? NullWallId, byte[] TileTypes, byte[] WallTypes, byte[] Liquids, byte[] BrightnessAndInvisibility, byte[] WireState);
	private sealed record StructureWritePlan(bool[] WriteTiles, bool[] WriteWalls, bool[] MutationCells, int MutationCellCount, Rectangle MutationBounds, int? TerrainAnchorY, Rectangle? FocusBounds, int DiscardedMutationCellCount);
}
