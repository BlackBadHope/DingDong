using System;
using System.Buffers;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

// Owner item 6 / E/H review 28.10 (root cause at ClassifySourceCell).
// ClassifySourceCell marks every non-terrain tile as authored construction, which
// includes ores, moss, gems, granite/marble, plants, vines, pots, piles and natural
// trees of the construction world. WriteLayerResolution grows support (20) and
// clearance (8) around those seeds, so a ground scene kept its whole native slab:
// a rectangle of the source biome with flat top, box sides and sky-coloured holes
// (and with it the foreign-biome decoration: jungle vines in forest, etc.).
//
// The StructureWritePlan is deliberately NOT changed: the planner, mutation mask,
// TerrainAnchorY/FocusBounds, liquid admission and reservations read it, so the
// candidate pools and the chosen sites stay byte-identical (61/61 is preserved by
// construction). Instead Generate writes a subset of the plan:
//  (1) distances are measured from core construction only (not from ambient
//      decoration); retained natural ground beyond SupportRadius and dry clearance
//      air beyond ClearanceRadius of core and of kept decoration (trees, plants,
//      lakes) are left to the host, unless enclosed by construction;
//  (2) whole objects are kept or left together: trees per 8-connected component,
//      multi-tile frame objects (pots, piles, life crystals) per same-type
//      component, source liquid bodies per 4-connected component, each kept when
//      any member is anchored near core;
//  (3) detached specks and crop-cut fragments without housing (a cloud halved by the
//      frame, a cut canopy) are not written (port of corrected-patch-1);
//  (4) closed wall-less air pockets inside retained ground get a host natural wall
//      instead of rendering as sky (port of corrected-patch-3).
// Only ground/coastal anchors are trimmed; sky islands, caverns, mushroom and
// underworld scenes keep the reviewed full plan. The subset depends only on the
// asset, the plan and the profile, so InspectIntegrity recomputes (or reuses) it.
public static partial class DingDongAuthoredStructureLoader
{
	private const int AmbientSupportRadius = 3;
	private const int FragmentSpeckMaxCells = 24;
	private const int FragmentEdgeCutDivisor = 8;
	private const int MaxGroundPocketCells = 2000;
	private const int PocketCoreClearance = 2;

	private sealed class SceneGroundDrop
	{
		internal bool[] Tiles, Walls, PocketWalls;
		internal int DroppedGround, DroppedAmbient, DroppedClearance, DroppedFragments, PocketCells;
	}

	// One-entry cache: Generate and the post-framing RequireIntegrity of the same
	// scene run back to back, so the second call reuses the first result. At most
	// one scene's drop is retained; cleared with the transient caches.
	private static readonly object sceneGroundDropGate = new();
	private static StructureWritePlan lastSceneGroundDropPlan;
	private static SceneGroundDrop lastSceneGroundDrop;

	private static void ClearSceneGroundDropCache()
	{
		lock (sceneGroundDropGate) { lastSceneGroundDropPlan = null; lastSceneGroundDrop = null; }
	}

	private static bool TrimsRetainedGround(AuthoredDeploymentProfile profile) => BlendsWithHost(profile) &&
		profile.TargetAnchor is DeploymentTargetAnchor.SurfaceGroundContact or DeploymentTargetAnchor.SurfaceTerrainEnvelope
			or DeploymentTargetAnchor.CoastalWaterlineEnvelope;

	// Natural features of the construction world that ClassifySourceCell does not
	// list as terrain. Clouds, living wood/leaves, vanity trees and every brick or
	// furniture stay construction.
	private const byte AmbientNone = 0, AmbientMineral = 1, AmbientSmall = 2, AmbientTree = 3;

	private static byte AmbientKind(ushort type)
	{
		if (type < TileID.Sets.Ore.Length && TileID.Sets.Ore[type]) return AmbientMineral;
		return type switch
		{
			// Mineral: part of the ground mass, decided per cell like natural ground.
			TileID.Amethyst or TileID.Topaz or TileID.Sapphire or TileID.Emerald or TileID.Ruby or TileID.Diamond or
			TileID.GreenMoss or TileID.BrownMoss or TileID.RedMoss or TileID.BlueMoss or TileID.PurpleMoss or TileID.LavaMoss or
			TileID.KryptonMoss or TileID.XenonMoss or TileID.ArgonMoss or TileID.VioletMoss or TileID.RainbowMoss or
			TileID.Granite or TileID.Marble or TileID.Silt or TileID.Slush or TileID.DesertFossil => AmbientMineral,
			// Tall natural objects: whole 8-connected object kept or left to the host.
			TileID.Trees or TileID.PalmTree or TileID.MushroomTrees or TileID.Cactus or TileID.Bamboo or TileID.Seaweed => AmbientTree,
			// Small surface/cave objects: only where their anchor ground stays; multi-tile
			// frame objects are decided as one object (UnsplitFrameObjects).
			TileID.Plants or TileID.Plants2 or TileID.JunglePlants or TileID.JunglePlants2 or TileID.MushroomPlants or
			TileID.CorruptPlants or TileID.CrimsonPlants or TileID.HallowedPlants or TileID.HallowedPlants2 or
			TileID.AshPlants or TileID.Saplings or TileID.Sunflower or TileID.PlantDetritus or TileID.LifeFruit or
			TileID.Vines or TileID.JungleVines or TileID.HallowedVines or TileID.CrimsonVines or TileID.VineFlowers or
			TileID.CorruptVines or TileID.AshVines or TileID.MushroomVines or
			TileID.CorruptThorns or TileID.JungleThorns or TileID.CrimsonThorns or
			TileID.ImmatureHerbs or TileID.MatureHerbs or TileID.BloomingHerbs or TileID.DyePlants or
			TileID.LilyPad or TileID.Cattail or TileID.SeaOats or TileID.OasisPlants or TileID.BeachPiles or TileID.Coral or
			TileID.Stalactite or TileID.SmallPiles or TileID.LargePiles or TileID.LargePiles2 or TileID.Pots or
			TileID.Cobweb or TileID.Heart or TileID.ExposedGems => AmbientSmall,
			_ => AmbientNone
		};
	}

	private static bool IsAmbientNaturalDecoration(ushort type) => AmbientKind(type) != AmbientNone;

	// Host masonry a scene's natural ground must not flatten (vanilla pyramids are
	// not StructureMap reservations). Only natural writes are skipped.
	private static bool IsProtectedHostMasonry(ushort type) => type is
		TileID.SandstoneBrick or TileID.LihzahrdBrick or
		TileID.BlueDungeonBrick or TileID.GreenDungeonBrick or TileID.PinkDungeonBrick or
		TileID.CrackedBlueDungeonBrick or TileID.CrackedGreenDungeonBrick or TileID.CrackedPinkDungeonBrick;

	// Pooled scratch buffers (as LiquidSafety): rented arrays are longer than w*h and
	// every loop below is bounded by w*h, never by Length. Cleared on rent.
	private sealed class SceneScratch : IDisposable
	{
		private readonly List<bool[]> bools = new();
		private readonly List<byte[]> bytes = new();
		private readonly List<int[]> ints = new();
		internal bool[] Bools(int n) { bool[] a = ArrayPool<bool>.Shared.Rent(n); Array.Clear(a, 0, n); bools.Add(a); return a; }
		internal byte[] Bytes(int n) { byte[] a = ArrayPool<byte>.Shared.Rent(n); Array.Clear(a, 0, n); bytes.Add(a); return a; }
		internal int[] Ints(int n) { int[] a = ArrayPool<int>.Shared.Rent(n); Array.Clear(a, 0, n); ints.Add(a); return a; }
		public void Dispose()
		{
			foreach (bool[] a in bools) ArrayPool<bool>.Shared.Return(a);
			foreach (byte[] a in bytes) ArrayPool<byte>.Shared.Return(a);
			foreach (int[] a in ints) ArrayPool<int>.Shared.Return(a);
			bools.Clear(); bytes.Clear(); ints.Clear();
		}
	}

	private static SceneGroundDrop BuildSceneGroundDrop(StructureData data, StructureWritePlan plan, AuthoredDeploymentProfile profile)
	{
		if (!TrimsRetainedGround(profile) || plan.FocusBounds == null)
			return null;
		lock (sceneGroundDropGate)
		{
			// The drop depends on the profile only through TrimsRetainedGround (checked
			// above), so the plan instance identifies it.
			if (ReferenceEquals(lastSceneGroundDropPlan, plan)) return lastSceneGroundDrop;
			SceneGroundDrop drop = ComputeSceneGroundDrop(data, plan);
			lastSceneGroundDropPlan = plan; lastSceneGroundDrop = drop;
			return drop;
		}
	}

	private static SceneGroundDrop ComputeSceneGroundDrop(StructureData data, StructureWritePlan plan)
	{
		int w = data.Width, h = data.Height, total = checked(w * h);
		using var scratch = new SceneScratch();
		bool[] core = scratch.Bools(total), tree = scratch.Bools(total), housing = scratch.Bools(total), liquid = scratch.Bools(total);
		byte[] kind = scratch.Bytes(total);
		HashSet<int> roomNeeds = new(TileID.Sets.RoomNeeds.CountsAsChair);
		roomNeeds.UnionWith(TileID.Sets.RoomNeeds.CountsAsTable);
		roomNeeds.UnionWith(TileID.Sets.RoomNeeds.CountsAsTorch);
		roomNeeds.UnionWith(TileID.Sets.RoomNeeds.CountsAsDoor);
		for (int i = 0; i < total; i++)
		{
			SourceCell s = ReadSourceCell(data, i);
			bool mechanism = s.RedWire || s.BlueWire || s.GreenWire || s.YellowWire || s.HasActuator;
			bool constructionTile = plan.WriteTiles[i] && s.HasTile && !IsNaturalTerrainTile(s.TileType);
			byte ambient = constructionTile && !mechanism ? AmbientKind(s.TileType) : AmbientNone;
			kind[i] = ambient;
			tree[i] = ambient == AmbientTree;
			liquid[i] = plan.WriteTiles[i] && s.LiquidAmount > 0;
			if ((constructionTile && ambient == AmbientNone) || mechanism) core[i] = true;
			if (plan.WriteWalls[i] && s.WallType != WallID.None && !IsNaturalBackgroundWall(s.WallType)) core[i] = true;
			bool houseWall = s.WallType != WallID.None && s.WallType < Main.wallHouse.Length && Main.wallHouse[s.WallType];
			housing[i] = houseWall || (s.HasTile && roomNeeds.Contains(s.TileType));
		}

		const int support = AuthoredSceneEnvelope.SupportRadius, clearance = AuthoredSceneEnvelope.ClearanceRadius;
		byte[] coreDistance = BoundedDistances(w, h, core, support, scratch.Bytes(total));
		int[] component = scratch.Ints(total);
		bool[] keptTree = scratch.Bools(total);
		KeepAnchoredComponents(w, h, tree, coreDistance, component, keptTree, eightConnected: true);
		// tree is no longer needed: reuse it as the flood barrier.
		bool[] barrier = tree;
		for (int i = 0; i < total; i++) barrier[i] = core[i] || keptTree[i];
		bool[] exterior = FloodFromBorder(w, h, barrier, scratch.Bools(total));
		// Source lakes/sea planes are kept or left as one body (review 28.10): a lake cut
		// on a distance contour would drain into host air during liquid settle and change
		// liquid behaviour the planner's liquid admission never saw.
		for (int i = 0; i < total; i++) liquid[i] = liquid[i] && !core[i] && exterior[i];
		bool[] keptLiquid = scratch.Bools(total);
		KeepAnchoredComponents(w, h, liquid, coreDistance, component, keptLiquid, eightConnected: false);
		// Ground within a few tiles of a kept tree or kept lake stays (roots, lake bed).
		bool[] anchors = liquid; // liquid flags are folded into keptLiquid; reuse the buffer.
		for (int i = 0; i < total; i++) anchors[i] = keptTree[i] || keptLiquid[i];
		byte[] anchorDistance = BoundedDistances(w, h, anchors, AmbientSupportRadius, scratch.Bytes(total));

		var drop = new SceneGroundDrop { Tiles = new bool[total], Walls = new bool[total], PocketWalls = new bool[total] };
		// Pass 1: solid matter (ground, lakes, decoration). Kept trees and kept lakes are
		// decoration for the clearance pass even though they are not exterior cells.
		bool[] keptDecoration = scratch.Bools(total);
		for (int i = 0; i < total; i++)
		{
			if (keptTree[i] || keptLiquid[i]) { keptDecoration[i] = true; continue; }
			if (!plan.WriteTiles[i] || core[i] || !exterior[i]) continue;
			SourceCell s = ReadSourceCell(data, i);
			bool nearAnchor = anchorDistance[i] <= AmbientSupportRadius;
			switch (kind[i])
			{
				case AmbientTree:
					drop.Tiles[i] = true; drop.DroppedAmbient++; // whole unanchored tree
					break;
				case AmbientMineral:
					if (coreDistance[i] > support && !nearAnchor) { drop.Tiles[i] = true; drop.DroppedAmbient++; }
					break;
				case AmbientSmall:
					// Kept only where the ground it hangs from or stands on is kept too.
					if (coreDistance[i] > support - AmbientSupportRadius && !nearAnchor) { drop.Tiles[i] = true; drop.DroppedAmbient++; }
					else keptDecoration[i] = true;
					break;
				default:
					if ((s.HasTile && IsNaturalTerrainTile(s.TileType)) || s.LiquidAmount > 0)
						if (coreDistance[i] > support && !nearAnchor) { drop.Tiles[i] = true; drop.DroppedGround++; }
					break;
			}
		}
		UnsplitFrameObjects(data, plan, kind, drop, keptDecoration, component);
		// Pass 2: clearance air and natural walls, around core and kept trees/plants/lakes.
		byte[] decorationDistance = BoundedDistances(w, h, keptDecoration, clearance, scratch.Bytes(total));
		for (int i = 0; i < total; i++)
		{
			if (core[i] || keptTree[i] || !exterior[i]) continue;
			SourceCell s = ReadSourceCell(data, i);
			bool clearOfAll = coreDistance[i] > clearance && decorationDistance[i] > clearance;
			if (plan.WriteTiles[i] && !drop.Tiles[i] && !s.HasTile && s.LiquidAmount == 0 && clearOfAll)
			{
				drop.Tiles[i] = true;
				drop.DroppedClearance++;
			}
			if (plan.WriteWalls[i] && (s.WallType == WallID.None || IsNaturalBackgroundWall(s.WallType)))
				drop.Walls[i] = drop.Tiles[i] || (!plan.WriteTiles[i] && clearOfAll); // kept ground keeps its wall
		}
		DropDetachedFragments(data, plan, drop, housing, component, scratch.Bools(total));
		MarkWalllessGroundPockets(data, plan, drop, housing, coreDistance, component);
		// Invariant: housing walls and room needs are never removed by the blend
		// (unreachable by the rules above; enforced instead of trusted).
		for (int i = 0; i < total; i++)
			if (housing[i]) { drop.Tiles[i] = false; drop.Walls[i] = false; }
		return drop;
	}

	// Labels components of member cells (8- or 4-connected) and marks a whole
	// component kept when any member is within SupportRadius of core.
	private static void KeepAnchoredComponents(int w, int h, bool[] member, byte[] coreDistance, int[] component, bool[] kept, bool eightConnected)
	{
		int total = w * h;
		Array.Fill(component, -1);
		var stack = new Stack<int>();
		var members = new List<int>();
		for (int start = 0; start < total; start++)
		{
			if (!member[start] || component[start] >= 0) continue;
			members.Clear();
			bool anchored = false;
			component[start] = start; stack.Push(start);
			while (stack.Count > 0)
			{
				int c = stack.Pop(); members.Add(c);
				anchored |= coreDistance[c] <= AuthoredSceneEnvelope.SupportRadius;
				int cx = c / h, cy = c % h;
				for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					if ((dx | dy) == 0 || (!eightConnected && dx != 0 && dy != 0)) continue;
					int nx = cx + dx, ny = cy + dy;
					if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
					int n = nx * h + ny;
					if (member[n] && component[n] < 0) { component[n] = start; stack.Push(n); }
				}
			}
			if (anchored) foreach (int c in members) kept[c] = true;
		}
	}

	// Multi-tile frame objects (pots, piles, life crystals, sunflowers ...) straddling
	// the support contour must not be split: half an object over host ground is
	// broken later by RangeFrame. A same-type 8-connected group no larger than the
	// largest such object (3x4) is kept whole when any of its cells is kept (keeping
	// is the conservative choice). Larger groups are rows of 1x1 plants: per cell.
	private const int FrameObjectMaxWidth = 3, FrameObjectMaxHeight = 4;
	private static void UnsplitFrameObjects(StructureData data, StructureWritePlan plan, byte[] kind, SceneGroundDrop drop, bool[] keptDecoration, int[] component)
	{
		int w = data.Width, h = data.Height, total = w * h;
		Array.Fill(component, -1);
		var stack = new Stack<int>();
		var members = new List<int>();
		for (int start = 0; start < total; start++)
		{
			if (kind[start] != AmbientSmall || component[start] >= 0) continue;
			ushort type = ReadSourceCell(data, start).TileType;
			if (type >= Main.tileFrameImportant.Length || !Main.tileFrameImportant[type]) continue;
			members.Clear();
			bool anyKept = false;
			int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
			component[start] = start; stack.Push(start);
			while (stack.Count > 0)
			{
				int c = stack.Pop(); members.Add(c);
				anyKept |= !drop.Tiles[c];
				int cx = c / h, cy = c % h;
				minX = Math.Min(minX, cx); maxX = Math.Max(maxX, cx); minY = Math.Min(minY, cy); maxY = Math.Max(maxY, cy);
				for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					int nx = cx + dx, ny = cy + dy;
					if ((dx | dy) == 0 || nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
					int n = nx * h + ny;
					if (kind[n] == AmbientSmall && component[n] < 0 && plan.WriteTiles[n] && ReadSourceCell(data, n).TileType == type)
					{ component[n] = start; stack.Push(n); }
				}
			}
			if (!anyKept || maxX - minX >= FrameObjectMaxWidth || maxY - minY >= FrameObjectMaxHeight) continue;
			foreach (int c in members)
			{
				if (drop.Tiles[c]) { drop.Tiles[c] = false; drop.DroppedAmbient--; }
				keptDecoration[c] = true;
			}
		}
	}

	// Port of corrected-patch-1 onto the write-time subset. The largest plan component
	// (the scene body) and every component carrying housing walls or room needs stay.
	// Specks are dropped anywhere; larger pieces only when the crop's top or sides
	// cut them and they are small next to the body. Ground touching the bottom row
	// of the crop is never treated as a cut fragment.
	private static void DropDetachedFragments(StructureData data, StructureWritePlan plan, SceneGroundDrop drop, bool[] housing, int[] component, bool[] content)
	{
		int w = data.Width, h = data.Height, total = w * h;
		for (int i = 0; i < total; i++)
		{
			SourceCell s = ReadSourceCell(data, i);
			// Connectivity is judged on the full plan, as in corrected-patch-1: trimming
			// ground must not turn an edge lamp or fence into a "fragment".
			bool tile = plan.WriteTiles[i] && (s.HasTile || s.LiquidAmount > 0);
			bool wall = plan.WriteWalls[i] && s.WallType != WallID.None;
			content[i] = tile || wall;
		}
		Array.Fill(component, -1);
		var sizes = new List<int>();
		var cropCut = new List<bool>();
		var grounded = new List<bool>();
		var hasHousing = new List<bool>();
		var stack = new Stack<int>();
		for (int start = 0; start < total; start++)
		{
			if (!content[start] || component[start] >= 0) continue;
			int id = sizes.Count, size = 0;
			bool cut = false, bottom = false, home = false;
			component[start] = id; stack.Push(start);
			while (stack.Count > 0)
			{
				int c = stack.Pop(); size++;
				int cx = c / h, cy = c % h;
				if (cy == 0 || cx == 0 || cx == w - 1) cut = true;
				if (cy == h - 1) bottom = true;
				home |= housing[c];
				for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					int nx = cx + dx, ny = cy + dy;
					if ((dx | dy) == 0 || nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
					int n = nx * h + ny;
					if (content[n] && component[n] < 0) { component[n] = id; stack.Push(n); }
				}
			}
			sizes.Add(size); cropCut.Add(cut); grounded.Add(bottom); hasHousing.Add(home);
		}
		if (sizes.Count <= 1) return;
		int main = 0;
		for (int k = 1; k < sizes.Count; k++) if (sizes[k] > sizes[main]) main = k;
		bool[] remove = new bool[sizes.Count];
		for (int k = 0; k < sizes.Count; k++)
			remove[k] = k != main && !hasHousing[k] &&
				(sizes[k] <= FragmentSpeckMaxCells || cropCut[k] && !grounded[k] && (long)sizes[k] * FragmentEdgeCutDivisor < sizes[main]);
		for (int i = 0; i < total; i++)
		{
			if (component[i] < 0 || !remove[component[i]]) continue;
			if (plan.WriteTiles[i] && !drop.Tiles[i]) { drop.Tiles[i] = true; drop.DroppedFragments++; }
			if (plan.WriteWalls[i]) drop.Walls[i] = true;
		}
	}

	// Port of corrected-patch-3. Construction-world caves have no background wall;
	// written as air + WallID.None inside retained ground at surface altitude they
	// render as sky. Only pockets fully enclosed by written natural ground (no crop
	// border, no housing, no construction, not hugging core) get a host wall.
	private static void MarkWalllessGroundPockets(StructureData data, StructureWritePlan plan, SceneGroundDrop drop, bool[] housing, byte[] coreDistance, int[] component)
	{
		int w = data.Width, h = data.Height, total = w * h;
		bool Written(int i) => plan.WriteTiles[i] && !drop.Tiles[i];
		bool IsPocket(int i)
		{
			if (!Written(i) || !plan.WriteWalls[i] || drop.Walls[i]) return false;
			SourceCell c = ReadSourceCell(data, i);
			return !c.HasTile && c.WallType == WallID.None;
		}
		bool IsGround(int i)
		{
			if (!Written(i) || housing[i]) return false;
			SourceCell c = ReadSourceCell(data, i);
			if (!c.HasTile) return c.WallType != WallID.None && IsNaturalBackgroundWall(c.WallType);
			return IsNaturalTerrainTile(c.TileType) || IsAmbientNaturalDecoration(c.TileType);
		}
		Array.Fill(component, -1);
		var members = new List<int>();
		var stack = new Stack<int>();
		Span<int> next = stackalloc int[4];
		for (int start = 0; start < total; start++)
		{
			if (component[start] >= 0 || !IsPocket(start)) continue;
			members.Clear();
			bool closed = true;
			component[start] = start; stack.Push(start);
			while (stack.Count > 0)
			{
				int i = stack.Pop(); members.Add(i);
				int x = i / h, y = i % h;
				if (x == 0 || y == 0 || x == w - 1 || y == h - 1 || housing[i] || coreDistance[i] <= PocketCoreClearance) closed = false;
				next[0] = x > 0 ? i - h : -1; next[1] = x < w - 1 ? i + h : -1;
				next[2] = y > 0 ? i - 1 : -1; next[3] = y < h - 1 ? i + 1 : -1;
				foreach (int n in next)
				{
					if (n < 0 || component[n] >= 0) continue;
					if (IsPocket(n)) { component[n] = start; stack.Push(n); }
					else if (!IsGround(n)) closed = false;
				}
			}
			if (!closed || members.Count > MaxGroundPocketCells) continue;
			foreach (int i in members) drop.PocketWalls[i] = true;
			drop.PocketCells += members.Count;
		}
	}

	// 4-connected BFS distance capped at radius (byte.MaxValue = farther), the same
	// metric as AuthoredSceneEnvelope.Distances but a quarter of the memory.
	private static byte[] BoundedDistances(int w, int h, bool[] seeds, int radius, byte[] distance)
	{
		int total = w * h;
		Array.Fill(distance, byte.MaxValue, 0, total);
		var queue = new Queue<int>();
		for (int i = 0; i < total; i++)
			if (seeds[i]) { distance[i] = 0; queue.Enqueue(i); }
		while (queue.Count > 0)
		{
			int index = queue.Dequeue(), next = distance[index] + 1;
			if (next > radius) continue;
			int x = index / h, y = index % h;
			if (x > 0) Visit(index - h);
			if (x + 1 < w) Visit(index + h);
			if (y > 0) Visit(index - 1);
			if (y + 1 < h) Visit(index + 1);
			void Visit(int target)
			{
				if (distance[target] <= next) return;
				distance[target] = (byte)next; queue.Enqueue(target);
			}
		}
		return distance;
	}

	// Cells reachable from the crop border without crossing construction or kept
	// trees. Ground enclosed by construction (a ring, a courtyard) stays.
	private static bool[] FloodFromBorder(int w, int h, bool[] barrier, bool[] reached)
	{
		var stack = new Stack<int>();
		void Seed(int i) { if (!barrier[i] && !reached[i]) { reached[i] = true; stack.Push(i); } }
		for (int x = 0; x < w; x++) { Seed(x * h); Seed(x * h + h - 1); }
		for (int y = 0; y < h; y++) { Seed(y); Seed((w - 1) * h + y); }
		while (stack.Count > 0)
		{
			int i = stack.Pop(), x = i / h, y = i % h;
			if (x > 0) Seed(i - h);
			if (x + 1 < w) Seed(i + h);
			if (y > 0) Seed(i - 1);
			if (y + 1 < h) Seed(i + 1);
		}
		return reached;
	}
}
