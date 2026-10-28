using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

// Owner item 6 (28.10, E/H review): "snow into snow, corruption into corruption".
// The retained natural ground of a scene used to take the palette of the profile's
// TargetBiome everywhere, so a scene admitted on a biome seam (the planner accepts
// 60 % contour coverage) carried a rectangle of the wrong biome into the other side
// of the seam: snow over forest dirt, a lime jungle wedge in forest, a purity box
// inside corruption. The palette is now read from the host the scene is written
// into, cell by cell, before that cell is overwritten:
//  - the exact pre-write host tile when it is a biome-specific natural tile;
//  - otherwise (host air, stone, ores) the majority of a 36x36 host neighbourhood;
//  - otherwise the profile's TargetBiome (old behaviour).
// Placement, write plan and planner pools are untouched; only the type of natural
// tiles/walls the plan already writes changes. Deterministic: host state only.
public static partial class DingDongAuthoredStructureLoader
{
	internal enum HostEvilKind : byte { None, Corruption, Crimson }

	internal readonly record struct HostPalette(HomeBiome Base, HostEvilKind Evil);

	// Ocean scenes need sand, Truffle needs a real mushroom biome, underground and
	// underworld lore homes keep their reviewed palettes: they keep the target.
	private static bool UsesHostPalette(AuthoredDeploymentProfile profile) => BlendsWithHost(profile) &&
		profile.TargetBiome is HomeBiome.Forest or HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Jungle or HomeBiome.Hallow;

	// Review 28.10: the per-cell host family can be Desert for a non-desert scene (a
	// desert seam, an underground sand pocket). BiomePalette(Desert) turns grass and
	// snow into falling Sand, which would collapse from overhangs, cave ceilings and
	// ledges onto paths and rooms (InspectIntegrity skips falling tiles). Outside
	// Desert-target scenes the desert palette therefore uses non-falling HardenedSand.
	private static ushort PaletteTile(ushort type, HostPalette palette, HomeBiome target)
	{
		ushort tile = HostNaturalTile(type, palette.Base);
		if (palette.Base == HomeBiome.Desert && target != HomeBiome.Desert && tile == TileID.Sand)
			tile = TileID.HardenedSand;
		return EvilizeTile(tile, palette.Evil);
	}

	private static ushort PaletteWall(ushort type, HostPalette palette) =>
		EvilizeWall(HostNaturalWall(type, palette.Base), palette.Evil);

	// Base family of a host tile: 0 neutral/unknown, else the HomeBiome it implies.
	private const byte FamilyNone = 0, FamilyForest = 1, FamilySnow = 2, FamilyDesert = 3, FamilyJungle = 4;

	private static byte HostFamily(ushort type) => type switch
	{
		TileID.Dirt or TileID.Grass or TileID.ClayBlock or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.HallowedGrass => FamilyForest,
		TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce => FamilySnow,
		TileID.Sand or TileID.HardenedSand or TileID.Sandstone or
			TileID.Ebonsand or TileID.CorruptHardenedSand or TileID.CorruptSandstone or
			TileID.Crimsand or TileID.CrimsonHardenedSand or TileID.CrimsonSandstone or
			TileID.Pearlsand or TileID.HallowHardenedSand or TileID.HallowSandstone => FamilyDesert,
		TileID.Mud or TileID.JungleGrass or TileID.CorruptJungleGrass or TileID.CrimsonJungleGrass => FamilyJungle,
		_ => FamilyNone
	};

	private static HostEvilKind HostEvil(ushort type) => type switch
	{
		TileID.CorruptGrass or TileID.CorruptJungleGrass or TileID.Ebonstone or TileID.Ebonsand or TileID.CorruptHardenedSand or TileID.CorruptSandstone or TileID.CorruptIce => HostEvilKind.Corruption,
		TileID.CrimsonGrass or TileID.CrimsonJungleGrass or TileID.Crimstone or TileID.Crimsand or TileID.CrimsonHardenedSand or TileID.CrimsonSandstone or TileID.FleshIce => HostEvilKind.Crimson,
		_ => HostEvilKind.None
	};

	// Tiles corruption/crimson can convert (pure, evil and hallowed variants). The
	// block evil share is measured over these only: vanilla evil keeps dirt, mud and
	// snow as they are, so counting them would veto the evil palette on most of a
	// surface corruption (review 28.10).
	private static bool IsEvilConvertible(ushort type) => type is
		TileID.Grass or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.HallowedGrass or
		TileID.JungleGrass or TileID.CorruptJungleGrass or TileID.CrimsonJungleGrass or
		TileID.Stone or TileID.Ebonstone or TileID.Crimstone or TileID.Pearlstone or
		TileID.Sand or TileID.Ebonsand or TileID.Crimsand or TileID.Pearlsand or
		TileID.HardenedSand or TileID.CorruptHardenedSand or TileID.CrimsonHardenedSand or TileID.HallowHardenedSand or
		TileID.Sandstone or TileID.CorruptSandstone or TileID.CrimsonSandstone or TileID.HallowSandstone or
		TileID.IceBlock or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce;

	// A hallowed host cell is never painted evil from its neighbourhood.
	private static bool IsHostHallow(ushort type) => type is
		TileID.HallowedGrass or TileID.Pearlstone or TileID.Pearlsand or TileID.HallowHardenedSand or
		TileID.HallowSandstone or TileID.HallowedIce;

	private static bool IsHostNaturalSample(ushort type) => IsNaturalTerrainTile(type) || HostFamily(type) != FamilyNone;

	private static HomeBiome FamilyBiome(byte family, HomeBiome fallback) => family switch
	{
		FamilyForest => HomeBiome.Forest,
		FamilySnow => HomeBiome.Snow,
		FamilyDesert => HomeBiome.Desert,
		FamilyJungle => HomeBiome.Jungle,
		_ => fallback
	};

	private static byte TargetFamily(HomeBiome target) => target switch
	{
		HomeBiome.Snow => FamilySnow,
		HomeBiome.Desert => FamilyDesert,
		HomeBiome.Jungle => FamilyJungle,
		_ => FamilyForest
	};

	// Same conversion vanilla applies when corruption/crimson spreads: dirt, mud and
	// snow stay; grass, stone, sand family and ice take the evil variant.
	private static ushort EvilizeTile(ushort type, HostEvilKind evil) => evil switch
	{
		HostEvilKind.Corruption => type switch
		{
			TileID.Grass => TileID.CorruptGrass,
			TileID.JungleGrass => TileID.CorruptJungleGrass,
			TileID.Stone => TileID.Ebonstone,
			TileID.Sand => TileID.Ebonsand,
			TileID.HardenedSand => TileID.CorruptHardenedSand,
			TileID.Sandstone => TileID.CorruptSandstone,
			TileID.IceBlock => TileID.CorruptIce,
			_ => type
		},
		HostEvilKind.Crimson => type switch
		{
			TileID.Grass => TileID.CrimsonGrass,
			TileID.JungleGrass => TileID.CrimsonJungleGrass,
			TileID.Stone => TileID.Crimstone,
			TileID.Sand => TileID.Crimsand,
			TileID.HardenedSand => TileID.CrimsonHardenedSand,
			TileID.Sandstone => TileID.CrimsonSandstone,
			TileID.IceBlock => TileID.FleshIce,
			_ => type
		},
		_ => type
	};

	private static ushort EvilizeWall(ushort type, HostEvilKind evil) => evil switch
	{
		HostEvilKind.Corruption => type switch
		{
			WallID.GrassUnsafe => WallID.CorruptGrassUnsafe,
			WallID.Stone => WallID.EbonstoneUnsafe,
			WallID.HardenedSand => WallID.CorruptHardenedSand,
			WallID.Sandstone => WallID.CorruptSandstone,
			_ => type
		},
		HostEvilKind.Crimson => type switch
		{
			WallID.GrassUnsafe => WallID.CrimsonGrassUnsafe,
			WallID.Stone => WallID.CrimstoneUnsafe,
			WallID.HardenedSand => WallID.CrimsonHardenedSand,
			WallID.Sandstone => WallID.CrimsonSandstone,
			_ => type
		},
		_ => type
	};

	// Pre-write host survey around one scene. Small (a few thousand ints for the
	// largest 700x450 scene), sampled once per Generate, never a full-map pass.
	private sealed class HostBiomeField
	{
		private const int Block = 12, Pad = 24, Step = 2, Window = 1, MinFamilySamples = 6;
		private readonly int left, top, blocksX, blocksY;
		private readonly byte[] family;
		private readonly HostEvilKind[] evil;
		private readonly HomeBiome target;
		internal int HostCells, ForeignBaseCells, EvilCells;

		private HostBiomeField(int left, int top, int blocksX, int blocksY, byte[] family, HostEvilKind[] evil, HomeBiome target)
		{
			this.left = left; this.top = top; this.blocksX = blocksX; this.blocksY = blocksY;
			this.family = family; this.evil = evil; this.target = target;
		}

		internal static HostBiomeField Sample(Point origin, int width, int height, HomeBiome target)
		{
			int left = Math.Max(1, origin.X - Pad), top = Math.Max(1, origin.Y - Pad);
			int right = Math.Min(Main.maxTilesX - 1, origin.X + width + Pad);
			int bottom = Math.Min(Main.maxTilesY - 1, origin.Y + height + Pad);
			int blocksX = Math.Max(1, (right - left + Block - 1) / Block), blocksY = Math.Max(1, (bottom - top + Block - 1) / Block);
			int blocks = blocksX * blocksY;
			// [0..4] families, 5 corruption, 6 crimson, 7 evil-convertible total
			int[] counts = new int[blocks * 8];
			for (int x = left; x < right; x += Step)
			for (int y = top; y < bottom; y += Step)
			{
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile || !IsHostNaturalSample(tile.TileType)) continue;
				int b = ((x - left) / Block * blocksY + (y - top) / Block) * 8;
				counts[b + HostFamily(tile.TileType)]++;
				HostEvilKind kind = HostEvil(tile.TileType);
				if (kind == HostEvilKind.Corruption) counts[b + 5]++;
				else if (kind == HostEvilKind.Crimson) counts[b + 6]++;
				if (IsEvilConvertible(tile.TileType)) counts[b + 7]++;
			}
			byte preferred = TargetFamily(target);
			byte[] family = new byte[blocks];
			HostEvilKind[] evil = new HostEvilKind[blocks];
			Span<int> sum = stackalloc int[8];
			for (int bx = 0; bx < blocksX; bx++)
			for (int by = 0; by < blocksY; by++)
			{
				sum.Clear();
				for (int nx = Math.Max(0, bx - Window); nx <= Math.Min(blocksX - 1, bx + Window); nx++)
				for (int ny = Math.Max(0, by - Window); ny <= Math.Min(blocksY - 1, by + Window); ny++)
				{
					int b = (nx * blocksY + ny) * 8;
					for (int k = 0; k < 8; k++) sum[k] += counts[b + k];
				}
				byte best = FamilyNone; int bestCount = MinFamilySamples - 1;
				// Fixed order with the target first keeps ties deterministic and conservative.
				if (sum[preferred] > bestCount) { best = preferred; bestCount = sum[preferred]; }
				for (byte f = FamilyForest; f <= FamilyJungle; f++)
					if (f != preferred && sum[f] > bestCount) { best = f; bestCount = sum[f]; }
				family[bx * blocksY + by] = best;
				int evilSamples = sum[5] + sum[6];
				// Only where corruption/crimson holds at least half of the convertible
					// tiles of the neighbourhood (dirt, mud and snow do not vote).
				if (evilSamples >= MinFamilySamples && evilSamples * 2 >= sum[7])
					evil[bx * blocksY + by] = sum[6] > sum[5] ? HostEvilKind.Crimson : HostEvilKind.Corruption;
			}
			return new HostBiomeField(left, top, blocksX, blocksY, family, evil, target);
		}

		// Call with the host tile BEFORE the scene writes this cell.
		internal HostPalette At(Tile host, int x, int y)
		{
			int bx = Math.Clamp((x - left) / Block, 0, blocksX - 1), by = Math.Clamp((y - top) / Block, 0, blocksY - 1);
			int b = bx * blocksY + by;
			byte f = FamilyNone;
			HostEvilKind kind = HostEvilKind.None;
			bool hallow = false;
			if (host.HasTile)
			{
				f = HostFamily(host.TileType);
				kind = HostEvil(host.TileType);
				hallow = IsHostHallow(host.TileType);
			}
			// Family and evil are decided separately (review 28.10): an evil-neutral host
			// cell (dirt, mud, snow, plain stone, air) takes the block's evil, so dirt
			// under a corrupt-grass skin no longer keeps the scene pure.
			if (f == FamilyNone) f = family[b];
			if (kind == HostEvilKind.None && !hallow) kind = evil[b];
			HostPalette palette = new(FamilyBiome(f, target), kind);
			if (f != FamilyNone) HostCells++;
			if (f != FamilyNone && f != TargetFamily(target)) ForeignBaseCells++;
			if (kind != HostEvilKind.None) EvilCells++;
			return palette;
		}
	}
}
