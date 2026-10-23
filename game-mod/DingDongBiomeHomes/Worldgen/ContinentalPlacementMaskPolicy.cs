using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalPlacementMaskPolicy
{
	internal static bool[] CreateStructureOnly()
	{
		var allowed = new bool[TileLoader.TileCount];
		Array.Fill(allowed, true);
		return allowed;
	}

	internal static bool[] CreateNaturalTerrain()
	{
		var allowed = new bool[TileLoader.TileCount];
		for (int type = 0; type < allowed.Length; type++)
			allowed[type] = IsNatural(type) && !IsProtectedObject(type);
		return allowed;
	}

	internal static bool[] CreateDungeonPortalTerrain()
	{
		bool[] allowed = CreateNaturalTerrain();
		Allow(allowed, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick,
			TileID.PinkDungeonBrick, TileID.CrackedBlueDungeonBrick,
			TileID.CrackedGreenDungeonBrick, TileID.CrackedPinkDungeonBrick);
		return allowed;
	}

	private static bool IsNatural(int type)
	{
		bool vanilla = type < TileID.Count &&
			At(TileID.Sets.GeneralPlacementTiles, type) &&
			At(TileID.Sets.CanBeClearedDuringGeneration, type);
		return vanilla || At(TileID.Sets.Ore, type) ||
			At(TileID.Sets.CanBeClearedDuringOreRunner, type) ||
			At(TileID.Sets.Dirt, type) || At(TileID.Sets.Stone, type) ||
			At(TileID.Sets.Mud, type) || At(TileID.Sets.Snow, type) ||
			At(TileID.Sets.Ices, type) || At(TileID.Sets.IcesSnow, type) ||
			At(TileID.Sets.IcesSlush, type) ||
			At(TileID.Sets.isDesertBiomeSand, type) ||
			At(TileID.Sets.Grass, type) || At(TileID.Sets.Ash, type) ||
			At(TileID.Sets.IsATreeTrunk, type) || At(TileID.Sets.Leaves, type) ||
			At(TileID.Sets.IsVine, type) || At(TileID.Sets.VineThreads, type) ||
			At(TileID.Sets.ReverseVineThreads, type);
	}

	private static bool IsProtectedObject(int type) =>
		At(Main.tileFrameImportant, type) || At(Main.tileContainer, type) ||
		At(Main.tileSign, type) || At(TileID.Sets.IsAContainer, type) ||
		At(TileID.Sets.IsAMechanism, type) || At(TileID.Sets.IsATrigger, type) ||
		At(TileID.Sets.IsMultitile, type) || At(TileID.Sets.BasicChest, type) ||
		At(TileID.Sets.BasicChestFake, type) || At(TileID.Sets.BasicDresser, type);

	private static bool At(bool[] set, int type) =>
		type >= 0 && type < set.Length && set[type];

	private static void Allow(bool[] allowed, params ushort[] types)
	{
		foreach (ushort type in types)
			if (type < allowed.Length && !IsProtectedObject(type))
				allowed[type] = true;
	}
}
