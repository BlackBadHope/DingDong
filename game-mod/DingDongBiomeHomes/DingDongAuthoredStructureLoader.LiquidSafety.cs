using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private sealed record LiquidSafetyEnvelope(Rectangle Bounds, byte[] SourceFlags);
	// Weak keys follow the scoped write-plan cache lifetime; no persistent world map.
	private static readonly ConditionalWeakTable<StructureWritePlan, LiquidSafetyEnvelope> liquidSafetyEnvelopes = new();

	internal static int PrepareLiquidSafetySource(Mod mod, AuthoredPreset preset, AuthoredDeploymentProfile profile)
	{
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
		return liquidSafetyEnvelopes.GetValue(plan, _ => BuildLiquidSafetyEnvelope(data, plan)).SourceFlags.Length;
	}

	internal static bool HasAmbientLavaExposure(Mod mod, AuthoredPreset preset, AuthoredDeploymentProfile profile,
		Point origin, out Point threat)
	{
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
		LiquidSafetyEnvelope envelope = liquidSafetyEnvelopes.GetValue(plan, _ => BuildLiquidSafetyEnvelope(data, plan));
		Rectangle local = envelope.Bounds;
		Rectangle area = local; area.Offset(origin);
		area = Rectangle.Intersect(area, new Rectangle(1, 1, Main.maxTilesX - 2, Main.maxTilesY - 2));
		bool foundAmbient = false;
		for (int x = area.Left; x < area.Right; x++)
		for (int y = area.Top; y < area.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Lava) continue;
			int sx = x - origin.X, sy = y - origin.Y;
			// These cells will receive the source liquid plane. Authored lava is not
			// an ambient leak, and liquids replaced by the import cannot survive it.
			if (sx >= 0 && sy >= 0 && sx < data.Width && sy < data.Height && plan.WriteTiles[sx * data.Height + sy]) continue;
			foundAmbient = true;
			break;
		}
		threat = default;
		if (!foundAmbient) return false;
		byte[] cells = ArrayPool<byte>.Shared.Rent(checked(area.Width * area.Height));
		try
		{
			for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; y < area.Bottom; y++)
			{
				int sx = x - origin.X, sy = y - origin.Y;
				bool inSource = sx >= 0 && sy >= 0 && sx < data.Width && sy < data.Height;
				int sourceIndex = inSource ? sx * data.Height + sy : -1;
				bool writesTile = inSource && plan.WriteTiles[sourceIndex];
				Tile world = Main.tile[x, y];
				bool open = writesTile
					? (envelope.SourceFlags[sourceIndex] & LiquidIngressReachability.Open) != 0
					: !world.HasTile || world.IsActuated || LiquidCanPass(world.TileType);
				byte flags = open ? LiquidIngressReachability.Open : (byte)0;
				if (inSource) flags |= (byte)(envelope.SourceFlags[sourceIndex] & LiquidIngressReachability.Room);
				if (!writesTile && world.LiquidAmount > 0 && world.LiquidType == LiquidID.Lava)
					flags |= LiquidIngressReachability.Lava;
				cells[(x - area.X) * area.Height + y - area.Y] = flags;
			}
			if (!LiquidIngressReachability.CanReachRoom(cells, area.Width, area.Height, out int reached)) return false;
			threat = new Point(area.X + reached / area.Height, area.Y + reached % area.Height);
			return true;
		}
		finally { ArrayPool<byte>.Shared.Return(cells); }
	}

	private static bool LiquidCanPass(ushort type) => !Main.tileSolid[type] || Main.tileSolidTop[type] ||
		(type < TileID.Sets.ClearedOnWorldLoad.Length && TileID.Sets.ClearedOnWorldLoad[type]) ||
		// Native QuickWater temporarily ignores doors and these worldgen objects.
		type is 10 or 138 or 190 or 191 or 192 or 484 or 546;

	private static LiquidSafetyEnvelope BuildLiquidSafetyEnvelope(StructureData data, StructureWritePlan plan)
	{
		byte[] flags = new byte[checked(data.Width * data.Height)];
		for (int x = 0; x < data.Width; x++)
		for (int y = 0; y < data.Height; y++)
		{
			int i = x * data.Height + y;
			SourceCell cell = ReadSourceCell(data, i);
			if (!cell.HasTile || cell.State.IsActuated || LiquidCanPass(cell.TileType)) flags[i] |= LiquidIngressReachability.Open;
			if (cell.LiquidAmount > 0 && cell.LiquidType == LiquidID.Lava) flags[i] |= LiquidIngressReachability.Lava;
			if (plan.WriteWalls[i] && cell.WallType > 0 && cell.WallType < Main.wallHouse.Length && Main.wallHouse[cell.WallType])
				flags[i] |= LiquidIngressReachability.Room;
		}
		// A source tree/grass patch is part of the authored scene even without a
		// housing wall. Lava burns it from the open cell beside it, while the
		// solid ground itself is not traversable. Protect the contact cells too.
		for (int x = 0; x < data.Width; x++)
		for (int y = 0; y < data.Height; y++)
		{
			int i = x * data.Height + y;
			SourceCell cell = ReadSourceCell(data, i);
			if (!plan.WriteTiles[i] || !cell.HasTile || !IsLavaFragileSceneTile(cell.TileType)) continue;
			void Protect(int xx, int yy)
			{
				if (xx < 0 || yy < 0 || xx >= data.Width || yy >= data.Height) return;
				int adjacent = xx * data.Height + yy;
				flags[adjacent] |= LiquidIngressReachability.Room;
			}
			Protect(x, y); Protect(x - 1, y); Protect(x + 1, y); Protect(x, y - 1); Protect(x, y + 1);
		}
		Rectangle bounds = plan.MutationBounds;
		bounds.Inflate(Worldgen.ResidenceWriteSafety.Halo, Worldgen.ResidenceWriteSafety.Halo);
		// Keep the full vertical envelope: a high pocket can flow under a wall and
		// back up to its original head. Lava originating below all rooms cannot.
		return new LiquidSafetyEnvelope(bounds, flags);
	}

	private static bool IsLavaFragileSceneTile(ushort type) => Main.tileLavaDeath[type] ||
		type is TileID.Trees or TileID.Grass or TileID.GolfGrass or TileID.JungleGrass or
			TileID.MushroomGrass or TileID.HallowedGrass or TileID.CorruptGrass or TileID.CrimsonGrass;
}
