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
	private static ushort HostNaturalTile(ushort type, HomeBiome target) => type switch
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

	private static ushort HostNaturalWall(ushort type, HomeBiome target) => type switch
	{
		WallID.HallowedGrassUnsafe or WallID.CorruptGrassUnsafe or WallID.CrimsonGrassUnsafe => WallID.GrassUnsafe,
		WallID.PearlstoneBrickUnsafe or WallID.EbonstoneUnsafe or WallID.CrimstoneUnsafe => WallID.Stone,
		WallID.HallowHardenedSand or WallID.CorruptHardenedSand or WallID.CrimsonHardenedSand => WallID.HardenedSand,
		WallID.HallowSandstone or WallID.CorruptSandstone or WallID.CrimsonSandstone => WallID.Sandstone,
		WallID.MushroomUnsafe when target != HomeBiome.Mushroom => WallID.GrassUnsafe,
		_ => type
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
	private static int QuenchAmbientLava(StructureData data, StructureWritePlan plan, Point origin, AuthoredDeploymentProfile profile)
	{
		Rectangle asset = new(origin.X, origin.Y, data.Width, data.Height);
		Rectangle envelope = Worldgen.ResidenceWriteSafety.Bounds(profile.ProtectedFootprint(asset), profile.ProtectedPadding);
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
