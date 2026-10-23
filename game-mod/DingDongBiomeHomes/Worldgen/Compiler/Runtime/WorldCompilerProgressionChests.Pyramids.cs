using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerProgressionChests
{
	internal static void PlacePyramidsAfterArena(WorldCompilerPlan plan,
		WorldCompilerProgressionReceipt receipt)
	{
		if (receipt == null) throw new InvalidOperationException("Pyramid loot ran before world contracts.");
		if (receipt.Chests.Exists(chest => chest.Kind == "pyramid"))
			throw new InvalidOperationException("Pyramid loot placement repeated.");
		Mod provider = ModLoader.GetMod("FargowiltasSouls");
		Type arena = provider.Code.GetType("FargowiltasSouls.Content.WorldGeneration.CoffinArena", true);
		FieldInfo field = arena.GetField("Rectangle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (field?.GetValue(null) is not Rectangle excluded)
			throw new InvalidOperationException("Fargo coffin arena rectangle contract changed.");
		if (excluded.Width > 0 && excluded.Height > 0) excluded.Inflate(6, 6);
		int count = 0;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			if (room.Kind != WorldCompilerStructureKind.DesertPyramid) continue;
			var site = WorldCompilerPyramidChestSite.Find(room.Bounds, (x, y) =>
				!excluded.Intersects(new Rectangle(x - 1, y - 3, 4, 5)) && PyramidSiteClear(x, y));
			PlaceAt("pyramid", site.X, site.Y, Pyramid[count++ % Pyramid.Length], 1, receipt);
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_PYRAMID_CHEST|verdict=PASS|phase=after-coffin|at={site.X},{site.Y - 1}|terrainRepaired=False");
		}
		if (count != Pyramid.Length) throw new InvalidOperationException($"Pyramid loot count: {count}/3.");
		receipt.PyramidLootPending = false;
	}

	private static bool PyramidSiteClear(int x, int y)
	{
		for (int dx = -1; dx <= 2; dx++)
		for (int dy = -3; dy <= 0; dy++)
		{
			Tile tile = Main.tile[x + dx, y + dy];
			if (tile.HasTile || tile.LiquidAmount != 0 || tile.WallType != WallID.SandstoneBrick) return false;
		}
		for (int dx = 0; dx <= 1; dx++)
		{
			Tile floor = Main.tile[x + dx, y + 1];
			if (!floor.HasTile || !Main.tileSolid[floor.TileType] || floor.IsHalfBlock ||
				floor.Slope != SlopeType.Solid) return false;
		}
		return true;
	}
}
