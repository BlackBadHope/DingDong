using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Owner, 30.10: "NPC homes must stand in their own biomes, even if it is the Hallow". Before
// hardmode there is no Hallow, so a resident whose home biome is the Hallow (Nurse, Wizard...)
// got a forest house. Around such a ground-level home the host terrain now turns into a small
// natural Hallow patch with a ragged edge; the authored scene and every reserved structure
// keep their own blocks.
internal static class HomeBiomePatchBuilder
{
	private const int MarginX = 90, MarginY = 70;

	internal static void Build(Mod mod, Rectangle[] reservations)
	{
		int homes = 0; long cells = 0;
		var scenes = new System.Collections.Generic.List<Rectangle>();
		foreach (SettlementResidenceReport other in SettlementWorldState.Residences) scenes.Add(other.Area);
		foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
		{
			if (home.Biome != HomeBiome.Hallow) continue;
			Rectangle area = home.Area;
			// Floating sky homes have no host ground to convert.
			if (area.Bottom < Main.worldSurface - 200) continue;
			homes++;
			Point c = area.Center;
			double rx = area.Width / 2.0 + MarginX, ry = area.Height / 2.0 + MarginY;
			int seed = home.ReviewId * 7919;
			for (int x = Math.Max(5, (int)(c.X - rx)); x <= Math.Min(Main.maxTilesX - 6, (int)(c.X + rx)); x++)
			for (int y = Math.Max(5, (int)(c.Y - ry)); y <= Math.Min(Main.maxTilesY - 6, (int)(c.Y + ry)); y++)
			{
				if (Reserved(scenes, x, y) || Reserved(reservations, x, y)) continue;
				double dx = (x - c.X) / rx, dy = (y - c.Y) / ry;
				double edge = 1 - .28 * Wobble(Math.Atan2(dy, dx), seed);
				if (dx * dx + dy * dy > edge * edge) continue;
				Tile t = Main.tile[x, y];
				if (!t.HasTile) continue;
				ushort to = t.TileType switch
				{
					TileID.Stone => TileID.Pearlstone,
					TileID.Grass => TileID.HallowedGrass,
					TileID.Sand => TileID.Pearlsand,
					TileID.HardenedSand => TileID.HallowHardenedSand,
					TileID.Sandstone => TileID.HallowSandstone,
					TileID.IceBlock => TileID.HallowedIce,
					_ => t.TileType
				};
				if (to == t.TileType) continue;
				t.TileType = to; cells++;
			}
			WorldGen.RangeFrame((int)(c.X - rx), (int)(c.Y - ry), (int)(c.X + rx), (int)(c.Y + ry));
		}
		mod.Logger.Info($"DINGDONG_HOME_BIOME_PATCH|biome=Hallow|homes={homes}|cells={cells}");
	}

	private static double Wobble(double angle, int seed) =>
		.5 + .25 * Math.Sin(angle * 3 + seed % 17) + .15 * Math.Sin(angle * 7 + seed % 29) + .1 * Math.Sin(angle * 13 + seed % 11);

	private static bool Reserved(System.Collections.Generic.IEnumerable<Rectangle> reservations, int x, int y)
	{
		foreach (Rectangle r in reservations)
			if (r.Contains(x, y)) return true;
		return false;
	}
}
