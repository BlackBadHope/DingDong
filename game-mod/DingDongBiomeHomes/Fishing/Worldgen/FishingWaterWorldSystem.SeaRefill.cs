using System;
using System.Collections.Generic;
using System.Diagnostics;
using DingDongBiomeHomes.OceanExpeditions;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public sealed partial class FishingWaterWorldSystem
{
	// Rows under the sea level the refill may reach; the drained caverns end ~330 rows down.
	private const int RefillDepth = 900;

	// The non-open sea keeps its water body; dry cells joined to it from below up to the sea level
	// are flooded again (flood fill from the current water surface, four-directional like liquid).
	// Solid tiles, bubble membranes (dry air pockets), other liquids and protected structures stop it.
	private static void RefillSulphurSeas()
	{
		if (!Generating) return;
		bool[] allowed = naturalMask ?? ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
		foreach (FishingWaterRegion region in regions.ToArray())
		{
			if (region.Shape != FishingRegionShape.Ocean) continue;
			bool left = region.Bounds.Center.X < Main.maxTilesX / 2;
			if (OceanExpeditionWorldSystem.Active && OceanExpeditionWorldSystem.Layout.Left == left) continue;
			int level = OceanExpeditionWorldSystem.Active ? OceanExpeditionWorldSystem.Layout.SeaLevel : region.WaterY;
			var clock = Stopwatch.StartNew();
			var r = RefillSea(region, level, allowed);
			// The higher water touches new openings: plug them like the coast seal does.
			var seal = SealOcean(region, left, false, allowed);
			ModContent.GetInstance<FishingWaterWorldSystem>().Mod.Logger.Info(
				$"DINGDONG_SULPHUR_SEA_REFILL|side={(left ? "left" : "right")}|level={level}|waterY={region.WaterY}|filled={r.Filled}|protected={r.Protected}|seeds={r.Seeds}|capped={r.Capped}|plugged={seal.Plugged}|selfSealed={seal.Self}|ms={clock.ElapsedMilliseconds}");
		}
	}

	private static (int Filled, int Protected, int Seeds, bool Capped) RefillSea(FishingWaterRegion region, int level, bool[] allowed)
	{
		Rectangle b = region.Bounds;
		int x0 = Math.Max(2, b.Left), x1 = Math.Min(Main.maxTilesX - 2, b.Right);
		int y0 = Math.Max(2, level), y1 = Math.Min(Main.maxTilesY - 210, level + RefillDepth);
		if (x1 <= x0 || y1 <= y0) return default;
		bool Blocks(Tile t) => SealSolid(t) || (t.HasTile && t.TileType == TileID.Bubble);
		bool Water(Tile t) => t.LiquidAmount >= 200 && t.LiquidType == LiquidID.Water;
		// One flag per cell of the window (4666 x 900 on x10: ~4 MB), not a hash set at the memory peak.
		int width = x1 - x0;
		var seen = new bool[width * (y1 - y0)];
		bool Mark(int x, int y) { int i = (y - y0) * width + (x - x0); if (seen[i]) return false; seen[i] = true; return true; }
		var queue = new Queue<(int X, int Y)>();
		int filled = 0, prot = 0, seeds = 0; bool capped = false;
		// Seeds: water cells of the body with a dry, open cell above them.
		for (int x = x0; x < x1; x++)
			for (int y = y0 + 1; y < y1; y++)
			{
				Tile t = Main.tile[x, y];
				if (!Water(t) || Blocks(t)) continue;
				Tile up = Main.tile[x, y - 1];
				if (up.LiquidAmount == 0 && !Blocks(up) && Mark(x, y - 1)) { queue.Enqueue((x, y - 1)); seeds++; }
			}
		while (queue.Count > 0)
		{
			var (x, y) = queue.Dequeue();
			Tile t = Main.tile[x, y];
			if (Blocks(t) || (t.LiquidAmount > 0 && t.LiquidType != LiquidID.Water)) continue;
			if (!GenVars.structures.CanPlace(new Rectangle(x, y, 1, 1), allowed, 0)) { prot++; continue; }
			if (t.LiquidAmount < 255) { t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; filled++; }
			foreach (var (dx, dy) in SealDirs)
			{
				int nx = x + dx, ny = y + dy;
				if (nx < x0 || nx >= x1 || ny < y0 || ny >= y1) continue;
				Tile n = Main.tile[nx, ny];
				if (Water(n) && n.LiquidAmount == 255) continue;
				if (Mark(nx, ny)) queue.Enqueue((nx, ny));
			}
		}
		return (filled, prot, seeds, capped);
	}
}
