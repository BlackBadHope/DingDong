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
	private static bool IsNaturalTerrainTile(ushort type) => type is TileID.Dirt or TileID.Stone or TileID.Grass or TileID.ClayBlock or TileID.Mud or TileID.JungleGrass or TileID.MushroomGrass or TileID.Sand or TileID.HardenedSand or TileID.Sandstone or TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce or TileID.CorruptGrass or TileID.Ebonstone or TileID.Ebonsand or TileID.CorruptHardenedSand or TileID.CorruptSandstone or TileID.CorruptIce or TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimsand or TileID.CrimsonHardenedSand or TileID.CrimsonSandstone or TileID.FleshIce or TileID.HallowedGrass or TileID.Pearlstone or TileID.Pearlsand or TileID.HallowHardenedSand or TileID.HallowSandstone or TileID.HallowedIce or TileID.Ash or TileID.Hellstone;
	private static bool IsNaturalBackgroundWall(ushort wallType) => wallType < Main.wallHouse.Length && !Main.wallHouse[wallType];
	private static byte[] RequireBlock<T>(Dictionary<string, byte[]> blocks, string key, int cellCount, string resourcePath)
		where T : unmanaged, ITileData
	{
		if (!blocks.TryGetValue(key, out byte[] data))
			throw new InvalidDataException($"Authored structure '{resourcePath}' is missing '{key}'.");
		int expected = checked(cellCount * Unsafe.SizeOf<T>());
		if (data.Length != expected)
			throw new InvalidDataException($"Authored structure '{resourcePath}' has invalid '{key}' length: {data.Length}, expected {expected}.");
		return data;
	}

	private static void SetEntry<T>(Tile tile, T value)
		where T : unmanaged, ITileData
	{
		ref T target = ref tile.Get<T>();
		target = value;
	}
}
