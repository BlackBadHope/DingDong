using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DingDongBiomeHomes.Fishing.Content;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public sealed partial class FishingWaterWorldSystem : ModSystem
{
	private static readonly List<FishingWaterRegion> regions = new();
	public static IReadOnlyList<FishingWaterRegion> Regions => regions;
	public static bool ExpandedWorld { get; private set; }
	internal static bool Generating { get; private set; }
	internal static bool AbyssLeft { get; set; }
	public static int TreasureSites { get; private set; }
	internal static int SulphurWidth { get; set; }
	private static bool[] naturalMask;

	public override void PreWorldGen()
	{
		regions.Clear(); TreasureSites = SulphurWidth = 0; ExpandedWorld = false;
		Generating = ContinentalWorldDimensions.IsContinental && !Main.remixWorld && !Main.notTheBeesWorld;
		if (Generating) FishingCalamityExpansion.RequireSupported();
		naturalMask = ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
	}
	public override void PostWorldGen()
	{
		if (Generating && regions.Count > 0) ExpandedWorld = true;
		Generating = false; naturalMask = null;
	}
	public override void OnWorldUnload() { regions.Clear(); ExpandedWorld = Generating = false; TreasureSites = SulphurWidth = 0; }

	// Called at the final task boundary, after all mods have supplied their passes.
	internal static void Attach(List<GenPass> tasks, ref double weight)
	{
		if (!ContinentalWorldDimensions.IsContinental || Main.remixWorld || Main.notTheBeesWorld) return;
		int beach = tasks.FindIndex(p => p.Name == "Beaches");
		if (beach < 0) throw new InvalidOperationException("Fishing worldgen: Beaches pass missing.");
		tasks.Insert(beach + 1, new PassLegacy("DingDong ocean expeditions", (p, _) => {
			p.Message = "Shaping ocean shelves and deepwater routes"; ShapeOceans();
		}, 1));
		int liquid = tasks.FindIndex(p => p.Name == "Settle Liquids");
		if (liquid < 0) throw new InvalidOperationException("Fishing worldgen: liquid settlement pass missing.");
		tasks.Insert(liquid, new PassLegacy("DingDong subterranean waters", (p, _) => {
			p.Message = "Carving cave lakes, rivers and sealed treasure grottoes"; ShapeUnderground();
		}, 1));
		weight += 2;
	}

	public static bool TryGetHabitat(int x, int y, out FishingHabitat habitat)
	{
		for (int i = regions.Count - 1; i >= 0; i--)
		{
			var region = regions[i];
			if (!region.Contains(x, y)) continue;
			habitat = region.Habitat;
			if (region.Shape == FishingRegionShape.Ocean)
			{
				int depth = Math.Max(0, y - region.WaterY);
				habitat = FishingPolicy.OceanHabitat(Math.Min(x, Main.maxTilesX - 1 - x), region.Bounds.Width + 10, depth, int.MaxValue);
			}
			return true;
		}
		habitat = default; return false;
	}
	internal static void RecordAbyss(bool left, int top, int bottom)
	{
		AbyssLeft = left;
		int width = 990;
		regions.Add(new(regions.Count, new Rectangle(left ? 1 : Main.maxTilesX - width, top, width - 1, bottom - top),
			top, FishingRegionShape.Abyss, FishingHabitat.Abyss, 0));
	}
	internal static void RecordExpeditionOcean(global::DingDongBiomeHomes.OceanExpeditions.OceanExpeditionLayout plan)
	{
		regions.RemoveAll(r => r.Shape == FishingRegionShape.Ocean && (r.Bounds.Center.X < Main.maxTilesX / 2) == plan.Left);
		int id = regions.Count == 0 ? 0 : regions.Max(r => r.Id) + 1;
		regions.Add(new(id, new Rectangle(plan.Left ? 20 : Main.maxTilesX - plan.Width, plan.SeaLevel,
			plan.Width - 20, plan.Depth + 1), plan.SeaLevel, FishingRegionShape.Ocean, FishingHabitat.OpenOcean, plan.Seed) { OceanProfileDepth = plan.Depth });
	}

	private static void ShapeOceans()
	{
		if (!Generating) return;
		int nativeWidth = Main.maxTilesX == 4200 ? 370 : Main.maxTilesX == 6400 ? 445 : (int)(Main.maxTilesX / 16.8f);
		int width = FishingWaterGeometry.OceanWidth(Main.maxTilesX, nativeWidth);
		for (int side = 0; side < 2; side++)
		{
			bool left = side == 0;
			int probeX = left ? 100 : Main.maxTilesX - 101;
			int waterY = FindWater(probeX);
			if (waterY < 0) continue;
			int oldBottom = waterY;
			while (oldBottom < Main.maxTilesY - 60 && FishingEnvironment.WaterAt(probeX, oldBottom + 1)) oldBottom++;
			int depth = FishingWaterGeometry.OceanDepth(Math.Max(80, oldBottom - waterY), Main.maxTilesY);
			int seed = WorldGen.genRand.Next();
			var bed = ShapeOceanColumns(left, width, waterY, depth, seed);
			regions.Add(new(regions.Count, new Rectangle(left ? 10 : Main.maxTilesX - width, waterY, width - 10,
				Math.Max(depth + depth / 4, bed.Deepest - waterY + 1)), waterY, FishingRegionShape.Ocean, FishingHabitat.OpenOcean, seed) { OceanProfileDepth = depth });
		}
		NativeCoastalBiomeSystem.CaptureGeneratedBounds();
	}
	private static int FindWater(int x)
	{
		for (int y = 40; y < Math.Min(Main.maxTilesY - 100, (int)Main.worldSurface + 400); y++)
			if (FishingEnvironment.WaterAt(x, y) && FishingEnvironment.WaterAt(x, y + 4)) return y;
		return -1;
	}
	private static void ShapeUnderground()
	{
		if (!Generating) return;
		int count = Math.Clamp(Main.maxTilesX / 700, 8, 40);
		int margin = Main.maxTilesX / 6 + 120;
		for (int i = 0; i < count; i++)
		{
			bool river = i % 3 == 0;
			for (int attempt = 0; attempt < 24; attempt++)
			{
				// V03 28.10: lakes wider than tall and shallower river beds; the old
				// 100-190 x 150-220 bounds made upright egg-shaped pockets (17.webp).
				int width = river ? WorldGen.genRand.Next(240, 420) : WorldGen.genRand.Next(180, 320);
				int height = river ? WorldGen.genRand.Next(130, 180) : WorldGen.genRand.Next(90, 150);
				bool upper = (i / 3) % 2 == 0;
				if (upper) height = Math.Min(height, Math.Max(100, (int)(Main.rockLayer - Main.worldSurface) - 140));
				int minY = upper ? (int)Main.worldSurface + 60 : (int)Main.rockLayer + 60;
				int maxY = upper ? (int)Main.rockLayer - height - 20 : NativeUnderworldSystem.Layer - height - 180;
				if (maxY <= minY) continue;
				Rectangle bounds = new(WorldGen.genRand.Next(margin, Main.maxTilesX - margin - width),
					WorldGen.genRand.Next(minY, maxY), width, height);
				var region = new FishingWaterRegion(regions.Count, bounds, bounds.Top + height / 3,
					river ? FishingRegionShape.River : FishingRegionShape.Lake,
					river ? FishingHabitat.UndergroundRiver : FishingHabitat.CaveLake, WorldGen.genRand.Next());
				if (!GenVars.structures.CanPlace(region.PlacementBounds, naturalMask, 12)) continue;
				Carve(region);
				regions.Add(region);
				GenVars.structures.AddProtectedStructure(region.PlacementBounds, 8);
				break;
			}
		}
	}
	private static void Carve(FishingWaterRegion r)
	{
		Rectangle footprint = r.PlacementBounds;
		// V03 28.10: the lobed Contains costs trig; evaluate it once per cell, not 25x per wall test.
		int fw = footprint.Width, fh = footprint.Height;
		bool[] inside = new bool[fw * fh];
		for (int x = 0; x < fw; x++)
			for (int y = 0; y < fh; y++) inside[x * fh + y] = r.Contains(footprint.Left + x, footprint.Top + y);
		const int seal = FishingWaterRegion.BasinWallThickness;
		for (int x = footprint.Left; x < footprint.Right; x++)
			for (int y = footprint.Top; y < footprint.Bottom; y++)
			{
				int ix = x - footprint.Left, iy = y - footprint.Top;
				if (!inside[ix * fh + iy])
				{
					// Retain an impermeable basin below its waterline, even where it
					// intersects an old natural cave. Access passages remain above water.
					if (y < r.WaterY) continue;
					bool wall = false;
					for (int dx = -seal; dx <= seal && !wall; dx++)
						for (int dy = -seal; dy <= seal && !wall; dy++)
						{
							int jx = ix + dx, jy = iy + dy;
							wall = jx >= 0 && jx < fw && jy >= 0 && jy < fh && inside[jx * fh + jy];
						}
					if (!wall) continue;
					Tile bank = Main.tile[x, y];
					// V03 28.10: keep the host's own solid ground (dirt, mud, ore...) so the
					// basin has no drawn stone outline; only gaps, sand and props become stone.
					if (bank.HasTile && !bank.IsActuated && Main.tileSolid[bank.TileType] && !Main.tileSolidTop[bank.TileType] &&
						!Main.tileSand[bank.TileType] && !TileID.Sets.Falling[bank.TileType])
					{ bank.LiquidAmount = 0; continue; }
					bank.ClearTile(); bank.HasTile = true;
					bank.TileType = TileID.Stone; bank.LiquidAmount = 0;
					continue;
				}
				Tile tile = Main.tile[x, y];
				tile.ClearTile(); tile.LiquidAmount = 0;
				int surface = r.WaterY;
				if (y >= surface) { tile.LiquidType = LiquidID.Water; tile.LiquidAmount = 255; }
			}
		// A sealed pocket beside each waterway: exploration and digging, not repeatable catch rewards.
		int cx = r.Bounds.Center.X, cy = r.Bounds.Bottom - 8;
		for (int x = cx - 7; x <= cx + 7; x++)
			for (int y = cy - 5; y <= cy + 3; y++)
			{
				Tile t = Main.tile[x, y]; t.ClearTile(); t.LiquidAmount = 0;
				if (x == cx - 7 || x == cx + 7 || y == cy + 3 || y == cy - 5)
				{ t.HasTile = true; t.TileType = TileID.Stone; }
			}
		int chest = WorldGen.PlaceChest(cx, cy + 2, (ushort)TileID.Containers, notNearOtherChests: true, style: 1);
		if (chest >= 0 && Main.chest[chest] != null)
		{
			var inv = Main.chest[chest].item;
			int cache = FishingContentCatalog.GetItemType(r.Habitat == FishingHabitat.CaveLake ? "lake_cache" : "river_cache");
			inv[0].SetDefaults(cache); inv[0].stack = 1;
			inv[1].SetDefaults(ItemID.SilverCoin); inv[1].stack = WorldGen.genRand.Next(15, 45);
			inv[2].SetDefaults(ItemID.SpelunkerPotion); inv[2].stack = 2;
			TreasureSites++;
		}
		WorldGen.RangeFrame(footprint.Left, footprint.Top, footprint.Right - 1, footprint.Bottom - 1);
	}

	public override void SaveWorldData(TagCompound tag)
	{
		tag["FishingExpandedV1"] = ExpandedWorld;
		tag["FishingAbyssLeft"] = AbyssLeft;
		tag["FishingTreasureSites"] = TreasureSites;
		tag["FishingSulphurWidth"] = SulphurWidth;
		tag["FishingWaterRegions"] = regions.Select(r => new TagCompound {
			["id"] = r.Id, ["x"] = r.Bounds.X, ["y"] = r.Bounds.Y, ["w"] = r.Bounds.Width, ["h"] = r.Bounds.Height,
			["water"] = r.WaterY, ["shape"] = (int)r.Shape, ["habitat"] = (int)r.Habitat, ["seed"] = r.Seed,
			["oceanProfileDepth"] = r.OceanProfileDepth }).ToList();
	}
	public override void LoadWorldData(TagCompound tag)
	{
		regions.Clear(); Generating = false;
		ExpandedWorld = tag.GetBool("FishingExpandedV1"); AbyssLeft = tag.GetBool("FishingAbyssLeft");
		TreasureSites = tag.GetInt("FishingTreasureSites");
		SulphurWidth = Math.Clamp(tag.GetInt("FishingSulphurWidth"), 0, Main.maxTilesX / 6);
		foreach (var r in tag.GetList<TagCompound>("FishingWaterRegions").Take(256))
			AddValidated(new(r.GetInt("id"), new Rectangle(r.GetInt("x"), r.GetInt("y"), r.GetInt("w"), r.GetInt("h")),
				r.GetInt("water"), (FishingRegionShape)r.GetInt("shape"), (FishingHabitat)r.GetInt("habitat"), r.GetInt("seed")) {
					OceanProfileDepth = Math.Clamp(r.GetInt("oceanProfileDepth"), 0, Main.maxTilesY) });
	}
	private static void AddValidated(FishingWaterRegion r)
	{
		if (r.Bounds.Width <= 0 || r.Bounds.Height <= 0 || r.Bounds.Left < 0 || r.Bounds.Top < 0 ||
			r.Bounds.Right >= Main.maxTilesX || r.Bounds.Bottom >= Main.maxTilesY || !Enum.IsDefined(r.Shape) || !Enum.IsDefined(r.Habitat)) return;
		regions.Add(r);
	}
	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(ExpandedWorld); writer.Write(AbyssLeft); writer.Write(SulphurWidth); writer.Write((ushort)regions.Count);
		foreach (var r in regions)
		{ writer.Write(r.Id); writer.Write(r.Bounds.X); writer.Write(r.Bounds.Y); writer.Write(r.Bounds.Width); writer.Write(r.Bounds.Height);
			writer.Write(r.WaterY); writer.Write((byte)r.Shape); writer.Write((byte)r.Habitat); writer.Write(r.Seed); }
	}
	public override void NetReceive(BinaryReader reader)
	{
		ExpandedWorld = reader.ReadBoolean(); AbyssLeft = reader.ReadBoolean(); regions.Clear();
		SulphurWidth = Math.Clamp(reader.ReadInt32(), 0, Main.maxTilesX / 6);
		int count = reader.ReadUInt16();
		if (count > 256) throw new InvalidDataException("Fishing region count exceeds bound.");
		for (int i = 0; i < count; i++) AddValidated(new(reader.ReadInt32(), new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
			reader.ReadInt32(), (FishingRegionShape)reader.ReadByte(), (FishingHabitat)reader.ReadByte(), reader.ReadInt32()));
	}
}
