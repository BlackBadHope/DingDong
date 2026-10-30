using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Worldgen order search, 30.10: Calamity's SulphurousSea.ClearOutStrayTiles took ~170 of the
// ~181 s of "Sulphur Sea 2" on x10. For every cell of the biome it flood-fills that cell's group
// of sulphurous blocks whose dither chance is not above zero again (capped at 433 cells) and clears groups of 2..limit-1 cells
// (limit 50 in the upper 42% of the depth, 432 below). A group is the same whichever of its cells
// starts the fill, and clearing one group never changes another, so each group is filled once
// here and cleared at exactly the start cell, in exactly the point order, the original would use.
//
// DINGDONG_SS2_STRAY: "fast" (default since the 30.10 shadow run: 19.2M cells, 0 mismatches)
// replaces the method, "shadow" plans without writing, lets Calamity
// run and compares every eligible cell (planned clear == actually cleared), "off" leaves it alone.
internal sealed class SulphurSeaStrayTilesAccelerator : ModSystem
{
	private const int FillCap = 432;
	private static string mode = "fast";
	private static Func<int> yStart, biomeWidth, blockDepth;
	private static Func<int, int> actualX;
	private static Func<int, int, int, int, int, float> dither;
	private static Func<int, int, Tile> tileAt;
	private static HashSet<ushort> blockTypes;
	private static ushort wall;

	public override void Load()
	{
		mode = (Environment.GetEnvironmentVariable("DINGDONG_SS2_STRAY") ?? "fast").Trim().ToLowerInvariant();
		if (mode == "off" || !ModLoader.TryGetMod("CalamityMod", out Mod calamity)) return;
		try
		{
			const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			Type sea = calamity.Code.GetType("CalamityMod.World.SulphurousSea", true);
			Type utils = calamity.Code.GetType("CalamityMod.CalamityUtils", true);
			yStart = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("YStart", any).GetGetMethod(true));
			biomeWidth = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("BiomeWidth", any).GetGetMethod(true));
			blockDepth = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), sea.GetProperty("BlockDepth", any).GetGetMethod(true));
			actualX = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), sea.GetMethod("GetActualX", any));
			dither = (Func<int, int, int, int, int, float>)Delegate.CreateDelegate(typeof(Func<int, int, int, int, int, float>), sea.GetMethod("CalculateDitherChance", any));
			tileAt = (Func<int, int, Tile>)Delegate.CreateDelegate(typeof(Func<int, int, Tile>), utils.GetMethod("ParanoidTileRetrieval", any, new[] { typeof(int), typeof(int) }));
			MethodInfo clear = sea.GetMethod("ClearOutStrayTiles", any);
			MonoModHooks.Add(clear, ClearOutStrayTiles);
			Mod.Logger.Info($"DINGDONG_SS2_STRAY|state=ARMED|mode={mode}");
		}
		catch (Exception error)
		{
			mode = "off";
			Mod.Logger.Warn("DINGDONG_SS2_STRAY|state=DISABLED|reason=" + error.Message);
		}
	}

	public override void Unload() { yStart = biomeWidth = blockDepth = null; actualX = null; dither = null; tileAt = null; blockTypes = null; }

	private static void ClearOutStrayTiles(Action orig)
	{
		if (mode == "off" || !ResolveTypes()) { orig(); return; }
		var clock = Stopwatch.StartNew();
		bool write = mode == "fast";
		var planned = new List<Point>();
		var eligible = write ? null : new List<Point>();
		Run(write, planned, eligible, out int groups, out int cleared);
		long planMs = clock.ElapsedMilliseconds;
		if (write)
		{
			ModContent.GetInstance<SulphurSeaStrayTilesAccelerator>().Mod.Logger.Info($"DINGDONG_SS2_STRAY|mode=fast|groups={groups}|clearedGroups={cleared}|clearedCells={planned.Count}|ms={planMs}");
			return;
		}
		clock.Restart();
		orig();
		long origMs = clock.ElapsedMilliseconds;
		var plannedSet = new HashSet<Point>(planned);
		int mismatches = 0;
		Point first = default;
		foreach (Point p in eligible)
		{
			Tile t = Main.tile[p.X, p.Y];
			bool isCleared = !t.HasTile;
			if (isCleared != plannedSet.Contains(p)) { if (mismatches++ == 0) first = p; }
		}
		ModContent.GetInstance<SulphurSeaStrayTilesAccelerator>().Mod.Logger.Info(
			$"DINGDONG_SS2_STRAY|mode=shadow|groups={groups}|clearedGroups={cleared}|plannedCells={planned.Count}|eligibleCells={eligible.Count}" +
			$"|mismatches={mismatches}|first={first.X},{first.Y}|planMs={planMs}|originalMs={origMs}|verdict={(mismatches == 0 ? "IDENTICAL" : "DIFFERENT")}");
	}

	private static bool ResolveTypes()
	{
		if (blockTypes != null) return true;
		if (!ModContent.TryFind("CalamityMod/SulphurousSand", out ModTile sand) ||
			!ModContent.TryFind("CalamityMod/SulphurousSandstone", out ModTile sandstone) ||
			!ModContent.TryFind("CalamityMod/HardenedSulphurousSandstone", out ModTile hardened) ||
			!ModContent.TryFind("CalamityMod/SulphurousSandWall", out ModWall sandWall))
			return false;
		blockTypes = new HashSet<ushort> { sand.Type, sandstone.Type, hardened.Type };
		wall = sandWall.Type;
		return true;
	}

	private sealed class Group { public int Size; public bool Done; }

	private static void Run(bool write, List<Point> planned, List<Point> eligible, out int groups, out int clearedGroups)
	{
		int width = biomeWidth(), depth = blockDepth(), top = yStart();
		var groupOf = new Dictionary<Point, Group>();
		var fill = new List<Point>();
		var inFill = new HashSet<Point>();
		groups = 0; clearedGroups = 0;
		for (int i = 1; i < width; i++)
		{
			int x = actualX(i);
			for (int y = top; y < top + depth; y++)
			{
				var start = new Point(x, y);
				if (!groupOf.TryGetValue(start, out Group group))
				{
					if (!Eligible(x, y, width, top, depth)) continue;
					fill.Clear(); inFill.Clear();
					Attach(x, y, fill, inFill, width, top, depth);
					group = new Group { Size = fill.Count };
					groups++;
					foreach (Point p in fill) { groupOf[p] = group; eligible?.Add(p); }
				}
				if (group.Done) continue;
				// Same float expression as the original: (float)y < (float)YStart + (float)depth * 0.42f.
				int limit = (float)y < (float)top + (float)depth * 0.42f ? 50 : 432;
				if (group.Size < 2 || group.Size >= limit) continue;
				// Refill from this start so the cleared points come in the original's order.
				fill.Clear(); inFill.Clear();
				Attach(x, y, fill, inFill, width, top, depth);
				foreach (Point p in fill)
				{
					planned.Add(p);
					if (write)
						WorldUtils.Gen(p, new Shapes.Rectangle(1, 1), Actions.Chain(new Actions.ClearTile(true), new Actions.PlaceWall(wall, true), new Actions.SetLiquid(0, 255)));
				}
				group.Done = true;
				clearedGroups++;
			}
		}
	}

	private static bool Eligible(int x, int y, int width, int top, int depth)
	{
		Tile tile = tileAt(x, y);
		// The original adds a cell when the dither chance is NOT above zero (ble.un: <= 0 or NaN).
		return blockTypes.Contains(tile.TileType) && tile.HasTile && !(dither(width, top, top + depth, x, y) > 0f);
	}

	// Mirror of Calamity's getAttachedPoints: same checks, same cap, same recursion order.
	private static void Attach(int x, int y, List<Point> list, HashSet<Point> inList, int width, int top, int depth)
	{
		Tile tile = tileAt(x, y);
		var point = new Point(x, y);
		if (!blockTypes.Contains(tile.TileType) || !tile.HasTile || list.Count > FillCap || inList.Contains(point) || dither(width, top, top + depth, x, y) > 0f)
			return;
		list.Add(point); inList.Add(point);
		Attach(x + 1, y, list, inList, width, top, depth);
		Attach(x - 1, y, list, inList, width, top, depth);
		Attach(x, y + 1, list, inList, width, top, depth);
		Attach(x, y - 1, list, inList, width, top, depth);
	}
}
