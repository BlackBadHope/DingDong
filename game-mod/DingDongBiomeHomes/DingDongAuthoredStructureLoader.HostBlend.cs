using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private const int SupportBackfillDepth = 48;
	private const int SupportSoilDepth = 12;

	// Scene crops carry the biome of the construction world they were cut from.
	// Hallow or evil ground dropped onto a fresh purity surface reads as a painted
	// rectangle on the map, so retained natural terrain takes the host palette.
	private static ushort HostNaturalTile(ushort type, HomeBiome target) => BiomePalette(Purify(type, target), target);

	// Step 1: hallow and evil ground becomes its purity equivalent.
	private static ushort Purify(ushort type, HomeBiome target) => type switch
	{
		TileID.Pearlstone or TileID.Ebonstone or TileID.Crimstone => TileID.Stone,
		TileID.HallowedGrass or TileID.CorruptGrass or TileID.CrimsonGrass => TileID.Grass,
		TileID.Pearlsand or TileID.Ebonsand or TileID.Crimsand => TileID.Sand,
		TileID.HallowHardenedSand or TileID.CorruptHardenedSand or TileID.CrimsonHardenedSand => TileID.HardenedSand,
		TileID.HallowSandstone or TileID.CorruptSandstone or TileID.CrimsonSandstone => TileID.Sandstone,
		TileID.HallowedIce or TileID.CorruptIce or TileID.FleshIce => TileID.IceBlock,
		TileID.MushroomGrass when target != HomeBiome.Mushroom => TileID.Grass,
		_ => type
	};

	// Step 2 (owner item 6 / WORLDGEN-ISSUES E, 28.10): the retained natural ground of a
	// scene takes the palette of the biome it is deployed in, so a snow castle no longer
	// carries a grey stone-and-dirt rectangle into the snow and a jungle manor no longer
	// sits on a forest-dirt block. Only the plain natural materials are swapped; ores,
	// bricks and furniture are never touched (IsNaturalTerrainTile gates the call).
	private static ushort BiomePalette(ushort type, HomeBiome target) => target switch
	{
		HomeBiome.Snow => type switch
		{
			TileID.Dirt or TileID.Grass or TileID.Mud or TileID.JungleGrass or TileID.Sand or TileID.HardenedSand => TileID.SnowBlock,
			TileID.Stone or TileID.Sandstone => TileID.IceBlock,
			_ => type
		},
		HomeBiome.Desert => type switch
		{
			TileID.Grass or TileID.JungleGrass or TileID.SnowBlock => TileID.Sand,
			TileID.Dirt or TileID.Mud => TileID.HardenedSand,
			TileID.Stone or TileID.IceBlock => TileID.Sandstone,
			_ => type
		},
		HomeBiome.Jungle => type switch
		{
			TileID.Dirt or TileID.SnowBlock or TileID.Sand or TileID.HardenedSand => TileID.Mud,
			TileID.Grass => TileID.JungleGrass,
			TileID.IceBlock or TileID.Sandstone => TileID.Stone,
			_ => type
		},
		HomeBiome.Ocean => type switch
		{
			TileID.Dirt or TileID.Grass or TileID.Mud or TileID.JungleGrass or TileID.SnowBlock => TileID.Sand,
			TileID.IceBlock => TileID.Sandstone,
			_ => type
		},
		HomeBiome.Underworld => type switch
		{
			TileID.Dirt or TileID.Grass or TileID.Mud or TileID.JungleGrass or TileID.SnowBlock or TileID.Sand or TileID.HardenedSand => TileID.Ash,
			TileID.IceBlock or TileID.Sandstone => TileID.Stone,
			_ => type
		},
		HomeBiome.Mushroom => type switch
		{
			TileID.Dirt or TileID.SnowBlock or TileID.Sand => TileID.Mud,
			TileID.Grass or TileID.JungleGrass => TileID.MushroomGrass,
			_ => type
		},
		// Forest, Hallow (purity surface before hardmode) and Underground lore homes: forest palette.
		_ => type switch
		{
			TileID.SnowBlock or TileID.Mud or TileID.Sand or TileID.HardenedSand => TileID.Dirt,
			TileID.JungleGrass => TileID.Grass,
			TileID.IceBlock or TileID.Sandstone => TileID.Stone,
			_ => type
		}
	};

	private static ushort HostNaturalWall(ushort type, HomeBiome target) => WallPalette(type switch
	{
		WallID.HallowedGrassUnsafe or WallID.CorruptGrassUnsafe or WallID.CrimsonGrassUnsafe => WallID.GrassUnsafe,
		WallID.PearlstoneBrickUnsafe or WallID.EbonstoneUnsafe or WallID.CrimstoneUnsafe => WallID.Stone,
		WallID.HallowHardenedSand or WallID.CorruptHardenedSand or WallID.CrimsonHardenedSand => WallID.HardenedSand,
		WallID.HallowSandstone or WallID.CorruptSandstone or WallID.CrimsonSandstone => WallID.Sandstone,
		WallID.MushroomUnsafe when target != HomeBiome.Mushroom => WallID.GrassUnsafe,
		_ => type
	}, target);

	private static ushort WallPalette(ushort type, HomeBiome target) => target switch
	{
		HomeBiome.Snow => type switch
		{
			WallID.DirtUnsafe or WallID.GrassUnsafe or WallID.MudUnsafe or WallID.JungleUnsafe or WallID.HardenedSand => WallID.SnowWallUnsafe,
			WallID.Sandstone => WallID.IceUnsafe,
			_ => type
		},
		HomeBiome.Desert => type switch
		{
			WallID.DirtUnsafe or WallID.GrassUnsafe or WallID.MudUnsafe or WallID.JungleUnsafe or WallID.SnowWallUnsafe => WallID.HardenedSand,
			WallID.IceUnsafe => WallID.Sandstone,
			_ => type
		},
		HomeBiome.Jungle => type switch
		{
			WallID.DirtUnsafe or WallID.SnowWallUnsafe or WallID.HardenedSand => WallID.MudUnsafe,
			WallID.GrassUnsafe => WallID.JungleUnsafe,
			_ => type
		},
		HomeBiome.Ocean => type switch
		{
			WallID.DirtUnsafe or WallID.GrassUnsafe or WallID.MudUnsafe or WallID.JungleUnsafe or WallID.SnowWallUnsafe => WallID.HardenedSand,
			_ => type
		},
		HomeBiome.Underworld or HomeBiome.Mushroom => type,
		_ => type switch
		{
			WallID.SnowWallUnsafe or WallID.MudUnsafe or WallID.HardenedSand => WallID.DirtUnsafe,
			WallID.JungleUnsafe => WallID.GrassUnsafe,
			WallID.IceUnsafe or WallID.Sandstone => WallID.Stone,
			_ => type
		}
	};

	private static bool BlendsWithHost(AuthoredDeploymentProfile profile) => profile != null &&
		profile.BoundaryPolicy == DeploymentBoundaryPolicy.PreserveBoundaryConnectedExterior;

	private static bool BackfillsSupport(AuthoredDeploymentProfile profile) => BlendsWithHost(profile) &&
		profile.TargetAnchor is DeploymentTargetAnchor.SurfaceTerrainEnvelope or DeploymentTargetAnchor.SurfaceGroundContact;

	private static bool QuenchesLava(AuthoredDeploymentProfile profile) => BlendsWithHost(profile) &&
		profile.TargetAnchor is DeploymentTargetAnchor.SurfaceTerrainEnvelope or DeploymentTargetAnchor.SurfaceGroundContact
			or DeploymentTargetAnchor.CoastalWaterlineEnvelope or DeploymentTargetAnchor.SkyClearanceEnvelope;

	// Seeds such as For the Worthy leave lava pools on the surface. Settling later
	// drains them into neighbouring homes and burns furniture and lawns. Ambient lava
	// inside the write envelope becomes water, and lava arriving from outside then
	// meets it as obsidian instead of entering the rooms.
	private const int QuenchApronTiles = 12;
	private static int QuenchAmbientLava(StructureData data, StructureWritePlan plan, Point origin, AuthoredDeploymentProfile profile)
	{
		Rectangle asset = new(origin.X, origin.Y, data.Width, data.Height);
		Rectangle envelope = Worldgen.ResidenceWriteSafety.Bounds(profile.ProtectedFootprint(asset), profile.ProtectedPadding);
		// RC12 still saw lava in 16 scenes: pools just outside the envelope flow in when
		// the world settles. Quench an apron around it too (ambient liquid only).
		envelope.Inflate(QuenchApronTiles, QuenchApronTiles);
		envelope = Rectangle.Intersect(envelope, new Rectangle(1, 1, Main.maxTilesX - 2, Main.maxTilesY - 2));
		int quenched = 0;
		for (int x = envelope.Left; x < envelope.Right; x++)
		for (int y = envelope.Top; y < envelope.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Lava) continue;
			int sx = x - origin.X, sy = y - origin.Y;
			if (sx >= 0 && sy >= 0 && sx < data.Width && sy < data.Height && plan.WriteTiles[sx * data.Height + sy])
				continue; // authored liquid plane, possibly deliberate lava
			tile.LiquidType = LiquidID.Water;
			quenched++;
		}
		return quenched;
	}

	private static ushort SupportMaterial(ushort surface, int depth) => surface switch
	{
		TileID.Sand or TileID.HardenedSand or TileID.Sandstone => depth < SupportSoilDepth ? TileID.HardenedSand : TileID.Sandstone,
		TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce => depth < SupportSoilDepth ? TileID.SnowBlock : TileID.IceBlock,
		TileID.Mud or TileID.JungleGrass => TileID.Mud,
		TileID.Ash or TileID.Hellstone => TileID.Ash,
		TileID.Stone => TileID.Stone,
		_ => depth < SupportSoilDepth ? TileID.Dirt : TileID.Stone
	};

	// A scene's retained cliff often ends where its crop ended, leaving a sheer cut
	// or a ledge hanging over lower host ground. Grow natural support under the
	// lowest retained terrain of every column until the host ground is reached.
	private static int BackfillSurfaceSupport(StructureData data, StructureWritePlan plan, Point origin)
	{
		Rectangle bounds = plan.MutationBounds;
		int filled = 0, left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;
		int floorLimit = Math.Min(Main.maxTilesY - 300, (int)Main.worldSurface + 160);
		for (int x = bounds.Left; x < bounds.Right; x++)
		{
			int worldX = origin.X + x;
			int seam = -1; ushort surface = TileID.Dirt;
			for (int y = bounds.Bottom - 1; y >= bounds.Top; y--)
			{
				int index = x * data.Height + y;
				if (!plan.WriteTiles[index]) continue;
				Tile written = Main.tile[worldX, origin.Y + y];
				if (!written.HasTile || !IsNaturalTerrainTile(written.TileType) || !Main.tileSolid[written.TileType]) continue;
				seam = origin.Y + y; surface = written.TileType; break;
			}
			if (seam < 0) continue;
			for (int depth = 1; depth <= SupportBackfillDepth; depth++)
			{
				int worldY = seam + depth;
				if (worldY >= floorLimit) break;
				Tile tile = Main.tile[worldX, worldY];
				if (tile.HasTile || tile.LiquidAmount > 0) break;
				tile.HasTile = true;
				tile.TileType = SupportMaterial(surface, depth);
				filled++;
				left = Math.Min(left, worldX); right = Math.Max(right, worldX);
				top = Math.Min(top, worldY); bottom = Math.Max(bottom, worldY);
			}
		}
		if (filled > 0) WorldGen.RangeFrame(Math.Max(1, left - 1), Math.Max(1, top - 1), Math.Min(Main.maxTilesX - 2, right + 1), Math.Min(Main.maxTilesY - 2, bottom + 1));
		return filled;
	}
}
