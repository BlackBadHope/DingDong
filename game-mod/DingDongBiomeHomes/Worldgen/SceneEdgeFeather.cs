using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Owner, 30.10 ("коробки"): where a scene's own ground stood higher or lower than the land
// around it, its left and right edges ended in straight vertical walls. In a band at each side
// the ground line now eases from the host's surface to the scene's: natural ground above the
// eased line goes, empty cells under it are filled with the host's material. Buildings, their
// walls and anything within three tiles of a non-natural block stay exactly as authored.
internal static class SceneEdgeFeather
{
	private const int Band = 26, Fill = 40, Guard = 3;

	internal static void Apply(Mod mod)
	{
		int sides = 0; long removed = 0, filled = 0;
		foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
		{
			Rectangle a = home.Area;
			// Ground-level scenes only: sky islands float by design, cavern ones have no surface line.
			if (a.Bottom < Main.worldSurface - 200 || a.Top > Main.rockLayer) continue;
			foreach (bool left in new[] { true, false })
			{
				int outside = left ? a.Left - 1 : a.Right, inner = left ? a.Left + Band : a.Right - 1 - Band;
				if (outside < 10 || outside >= Main.maxTilesX - 10 || inner <= a.Left || inner >= a.Right - 1) continue;
				int hostTop = GroundTop(outside, a.Top - 60, a.Bottom + 60), sceneTop = GroundTop(inner, a.Top, a.Bottom);
				if (hostTop < 0 || sceneTop < 0 || Math.Abs(hostTop - sceneTop) < 3) continue;
				sides++;
				for (int d = 0; d < Band; d++)
				{
					int x = left ? a.Left + d : a.Right - 1 - d;
					double t = d / (double)Band; t = t * t * (3 - 2 * t);
					int target = (int)Math.Round(hostTop + (sceneTop - hostTop) * t);
					for (int y = a.Top; y < a.Bottom; y++)
					{
						Tile tile = Main.tile[x, y];
						if (y < target)
						{
							if (!tile.HasTile || !DingDongAuthoredStructureLoader.IsNaturalTerrainTile(tile.TileType) || NearBuilt(x, y)) continue;
							tile.HasTile = false;
							if (tile.WallType != WallID.None && !Main.wallHouse[tile.WallType]) tile.WallType = WallID.None;
							removed++;
						}
						else if (y < target + Fill)
						{
							if (tile.HasTile || tile.LiquidAmount > 0 || (tile.WallType != WallID.None && Main.wallHouse[tile.WallType]) || NearBuilt(x, y)) continue;
							tile.HasTile = true;
							tile.TileType = HostMaterial(outside, hostTop + (y - target));
							tile.Slope = SlopeType.Solid; tile.IsHalfBlock = false;
							filled++;
						}
					}
				}
				int from = left ? a.Left - 2 : a.Right - Band - 2, to = left ? a.Left + Band + 2 : a.Right + 2;
				WorldGen.RangeFrame(from, a.Top - 2, to, a.Bottom + 2);
			}
		}
		long canopies = 0;
		var homes = new System.Collections.Generic.List<Rectangle>();
		foreach (SettlementResidenceReport home in SettlementWorldState.Residences) homes.Add(home.Area);
		foreach (Rectangle area in homes)
		{
			Rectangle around = area; around.Inflate(80, 60);
			canopies += ClearFloatingCanopies(around, homes);
		}
		mod.Logger.Info($"DINGDONG_SCENE_EDGE_FEATHER|sides={sides}|removed={removed}|filled={filled}|floatingCanopyCells={canopies}");
	}

	private static bool Canopy(Tile t) => t.HasTile && t.TileType is TileID.LeafBlock or TileID.LivingWood or TileID.LivingMahoganyLeaves or TileID.LivingMahogany;

	// Owner, 30.10: a tree crown hung in the sky beside a house whose scene had cut its trunk.
	// A connected crown of living wood and leaves that touches no other solid block goes.
	// A crown reaching into any residence is that residence's own authored tree and stays.
	private static long ClearFloatingCanopies(Rectangle area, System.Collections.Generic.List<Rectangle> homes)
	{
		area = Rectangle.Intersect(area, new Rectangle(20, 20, Main.maxTilesX - 40, Main.maxTilesY - 40));
		var seen = new System.Collections.Generic.HashSet<int>();
		var stack = new System.Collections.Generic.Stack<Point>();
		var part = new System.Collections.Generic.List<Point>();
		long cleared = 0;
		for (int x = area.Left; x < area.Right; x++)
		for (int y = area.Top; y < area.Bottom; y++)
		{
			if (!Canopy(Main.tile[x, y]) || !seen.Add(x * Main.maxTilesY + y)) continue;
			part.Clear(); stack.Push(new Point(x, y)); bool anchored = false;
			while (stack.Count > 0 && part.Count < 6000)
			{
				Point p = stack.Pop(); part.Add(p);
				if (!anchored) foreach (Rectangle home in homes) if (home.Contains(p)) { anchored = true; break; }
				foreach (Point n in new[] { new Point(p.X + 1, p.Y), new Point(p.X - 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X, p.Y - 1) })
				{
					if (n.X < 5 || n.Y < 5 || n.X >= Main.maxTilesX - 5 || n.Y >= Main.maxTilesY - 5) { anchored = true; continue; }
					Tile t = Main.tile[n.X, n.Y];
					if (Canopy(t)) { if (seen.Add(n.X * Main.maxTilesY + n.Y)) stack.Push(n); }
					else if (t.HasTile && Main.tileSolid[t.TileType]) anchored = true;
				}
			}
			if (anchored || stack.Count > 0) { stack.Clear(); continue; }
			foreach (Point p in part) { Tile gone = Main.tile[p.X, p.Y]; gone.HasTile = false; cleared++; }
		}
		if (cleared > 0) WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
		return cleared;
	}

	private static int GroundTop(int x, int from, int to)
	{
		for (int y = Math.Max(10, from); y < Math.Min(Main.maxTilesY - 10, to); y++)
		{
			Tile t = Main.tile[x, y];
			if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) return y;
		}
		return -1;
	}

	private static ushort HostMaterial(int x, int y)
	{
		y = Math.Clamp(y, 10, Main.maxTilesY - 10);
		Tile t = Main.tile[x, y];
		if (t.HasTile && DingDongAuthoredStructureLoader.IsNaturalTerrainTile(t.TileType))
			return t.TileType is TileID.Grass or TileID.JungleGrass or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.HallowedGrass or TileID.MushroomGrass
				? (t.TileType == TileID.JungleGrass || t.TileType == TileID.MushroomGrass ? TileID.Mud : TileID.Dirt) : t.TileType;
		return TileID.Dirt;
	}

	private static bool NearBuilt(int x, int y)
	{
		for (int dx = -Guard; dx <= Guard; dx++)
		for (int dy = -Guard; dy <= Guard; dy++)
		{
			Tile t = Main.tile[x + dx, y + dy];
			if (t.HasTile && !DingDongAuthoredStructureLoader.IsNaturalTerrainTile(t.TileType) && !Main.tileCut[t.TileType] && t.TileType != TileID.Trees) return true;
			if (t.WallType != WallID.None && Main.wallHouse[t.WallType]) return true;
		}
		return false;
	}
}
