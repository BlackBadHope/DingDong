using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalNaturalTiles
{
	internal static bool[] CreatePlacementMask(bool allowTrees)
	{
		var allowed = new bool[TileLoader.TileCount];
		int count = Math.Min(Math.Min(TileID.Count, allowed.Length), TileID.Sets.GeneralPlacementTiles.Length);
		Array.Copy(TileID.Sets.GeneralPlacementTiles, allowed, count);
		if (allowTrees)
		{
			Allow(allowed, TileID.Trees, TileID.PalmTree, TileID.RichMahogany,
				TileID.LivingMahogany, TileID.LivingMahoganyLeaves, TileID.Plants,
				TileID.Plants2, TileID.JunglePlants, TileID.JunglePlants2, TileID.JungleVines);
		}
		return allowed;
	}

	internal static bool[] CreateDungeonPortalMask()
	{
		bool[] allowed = CreatePlacementMask(allowTrees: false);
		Allow(allowed, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick,
			TileID.CrackedBlueDungeonBrick, TileID.CrackedGreenDungeonBrick, TileID.CrackedPinkDungeonBrick);
		return allowed;
	}

	internal static bool CanShape(ushort type) => type is
		TileID.Dirt or TileID.Grass or TileID.Stone or TileID.ClayBlock or
		TileID.Mud or TileID.JungleGrass or TileID.Sand or TileID.HardenedSand or
		TileID.Sandstone or TileID.SnowBlock or TileID.IceBlock or
		TileID.BreakableIce or TileID.Silt or TileID.Slush;

	internal static bool IsDungeonBrick(ushort type) => type is
		TileID.BlueDungeonBrick or TileID.GreenDungeonBrick or TileID.PinkDungeonBrick or
		TileID.CrackedBlueDungeonBrick or TileID.CrackedGreenDungeonBrick or TileID.CrackedPinkDungeonBrick;

	internal static bool IsDungeonWall(ushort type) => type is
		WallID.BlueDungeonUnsafe or WallID.GreenDungeonUnsafe or WallID.PinkDungeonUnsafe or
		WallID.BlueDungeonSlabUnsafe or WallID.GreenDungeonSlabUnsafe or WallID.PinkDungeonSlabUnsafe or
		WallID.BlueDungeonTileUnsafe or WallID.GreenDungeonTileUnsafe or WallID.PinkDungeonTileUnsafe;

	private static void Allow(bool[] allowed, params ushort[] types)
	{
		foreach (ushort type in types)
			if (type < allowed.Length)
				allowed[type] = true;
	}
}
