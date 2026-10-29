using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DingDongBiomeHomes.OceanExpeditions;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public sealed partial class FishingWaterWorldSystem
{
	// V03 29.10: owner 16.webp. ShapeOceanColumns (OceanBed.cs:38-57) widens both
	// seas to OceanWidth (4666 on x10) with water from WaterY against untouched
	// land: no inland dam, and beds lowered into chasms/caves open sideways. The
	// first "Settle Liquids" drains that sea over the inland face into lower
	// valleys and evil chasms. Before each settle (and once more after the coastal
	// providers on the non-open side) plug every dry opening the sea body touches.
	private const int ShoreSealFace = 48, ShoreSealWetCap = 262144;
	private static readonly (int X, int Y)[] SealDirs = { (-1, 0), (1, 0), (0, 1), (0, -1) };

	internal static void AttachShoreSeal(List<GenPass> tasks, ref double weight)
	{
		if (!ContinentalWorldDimensions.IsContinental || Main.remixWorld || Main.notTheBeesWorld) return;
		var sites = new List<(int At, string Name, bool Face)>();
		int settle = tasks.FindIndex(p => p.Name == "Settle Liquids");
		int again = tasks.FindIndex(p => p.Name == "Settle Liquids Again");
		int coast = tasks.FindLastIndex(p => p.Name is "Abyss" or "Sulphur Sea 2" or "DingDong open ocean");
		if (settle >= 0) sites.Add((settle, "DingDong shore seal", false));
		if (again >= 0) sites.Add((again, "DingDong shore seal again", false));
		if (coast >= 0) sites.Add((coast + 1, "DingDong shore seal coast", true));
		foreach (var site in sites.OrderByDescending(s => s.At))
		{
			string stage = site.Name; bool face = site.Face;
			tasks.Insert(site.At, new PassLegacy(stage, (p, _) => {
				p.Message = "Sealing sea shores and beds"; SealOceanShores(stage, face); }, 0.2));
			weight += 0.2;
		}
	}

	private static void SealOceanShores(string stage, bool face)
	{
		if (!Generating) return;
		bool[] allowed = naturalMask ?? ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
		foreach (FishingWaterRegion region in regions.ToArray())
		{
			if (region.Shape != FishingRegionShape.Ocean) continue;
			bool left = region.Bounds.Center.X < Main.maxTilesX / 2;
			// The open ocean has its own dam/shell around designed air grottos.
			if (OceanExpeditionWorldSystem.Active && OceanExpeditionWorldSystem.Layout.Left == left) continue;
			var clock = Stopwatch.StartNew();
			var r = SealOcean(region, left, face, allowed);
			ModContent.GetInstance<FishingWaterWorldSystem>().Mod.Logger.Info(
				$"DINGDONG_SHORE_SEAL|stage={stage}|side={(left ? "left" : "right")}|waterY={region.WaterY}|plugged={r.Plugged}|selfSealed={r.Self}|blocked={r.Blocked}|wetOutside={r.Wet}|capped={r.Capped}|ms={clock.ElapsedMilliseconds}");
		}
	}

	private static (int Plugged, int Self, int Blocked, int Wet, bool Capped) SealOcean(FishingWaterRegion region, bool left, bool face, bool[] allowed)
	{
		Rectangle b = region.Bounds;
		int top = Math.Max(2, region.WaterY - 120), bottom = Math.Min(b.Bottom, Main.maxTilesY - 2);
		int x0 = Math.Max(2, b.Left), x1 = Math.Min(Main.maxTilesX - 2, b.Right);
		if (face) { if (left) x0 = Math.Max(x0, x1 - ShoreSealFace); else x1 = Math.Min(x1, x0 + ShoreSealFace); }
		if (x1 <= x0 || bottom <= region.WaterY) return default;
		// Per column: the sea body runs from its first wet row (at or above
		// WaterY; Calamity may hold it higher) down to the first solid tile.
		int[] start = new int[x1 - x0], end = new int[x1 - x0];
		for (int x = x0; x < x1; x++)
		{
			int y = top;
			while (y < region.WaterY && Main.tile[x, y].LiquidAmount == 0) y++;
			start[x - x0] = y;
			while (y < bottom && !SealSolid(Main.tile[x, y])) y++;
			end[x - x0] = y;
		}
		bool Body(int x, int y)
		{
			if (x >= x0 && x < x1) return y >= start[x - x0] && y < end[x - x0];
			// Face mode: the rest of the band is sea, not a leak.
			return face && x >= b.Left && x < b.Right && y >= top && y < b.Bottom;
		}
		int plugged = 0, self = 0, blocked = 0; bool capped = false;
		var seen = new HashSet<long>(); var queue = new Queue<(int X, int Y)>();
		// Returns false when the wet cell itself had to become the seal.
		bool Probe(int x, int y)
		{
			foreach (var (dx, dy) in SealDirs)
			{
				int nx = x + dx, ny = y + dy;
				if (nx < 2 || nx >= Main.maxTilesX - 2 || ny < 2 || ny >= Main.maxTilesY - 2 || Body(nx, ny)) continue;
				Tile n = Main.tile[nx, ny];
				if (SealSolid(n)) continue;
				if (n.LiquidAmount > 0)
				{
					// V03 29.10: follow water only; lava/honey/shimmer contact stays vanilla.
					if (n.LiquidType != LiquidID.Water) continue;
					if (seen.Count >= ShoreSealWetCap) { capped = true; continue; }
					if (seen.Add(((long)nx << 32) | (uint)ny)) queue.Enqueue((nx, ny));
					continue;
				}
				if (dy < 0) continue; // dry air above water is not a leak
				if (GenVars.structures.CanPlace(new Rectangle(nx, ny, 1, 1), allowed, 0))
				{ ushort type = PlugTile(nx, ny, dx, dy, allowed); n.ClearTile(); n.HasTile = true; n.TileType = type; plugged++; continue; }
				// Never write into a reserved cell: seal this wet cell instead.
				// V03 29.10: unless the wet cell is itself reserved or holds an object
				// (chest, pot, coral): then leave the opening to its owner.
				if (!GenVars.structures.CanPlace(new Rectangle(x, y, 1, 1), allowed, 0)) { blocked++; continue; }
				Tile t = Main.tile[x, y]; t.ClearTile(); t.LiquidAmount = 0; t.HasTile = true; t.TileType = TileID.Stone; self++;
				return false;
			}
			return true;
		}
		for (int x = x0; x < x1; x++)
			for (int y = start[x - x0]; y < end[x - x0]; y++)
				if (Main.tile[x, y].LiquidAmount > 0) Probe(x, y);
		// Natural water already joined to the sea (cave pools, chasm puddles) is
		// the same body; follow it, bounded, and seal its dry openings too.
		while (queue.Count > 0)
		{
			var (x, y) = queue.Dequeue();
			if (Main.tile[x, y].LiquidAmount > 0 && !SealSolid(Main.tile[x, y])) Probe(x, y);
		}
		return (plugged, self, blocked, seen.Count, capped);
	}

	private static bool SealSolid(Tile t) => t.HasTile && !t.IsActuated && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];

	// Host material from the far side or below; grass becomes its soil. Never ore,
	// sand or a falling block (it would collapse and reopen the leak).
	private static ushort PlugTile(int x, int y, int dx, int dy, bool[] allowed)
	{
		foreach (var (px, py) in new[] { (x + dx, y + dy), (x, y + 1), (x - dx, y) })
		{
			if (px < 1 || px >= Main.maxTilesX - 1 || py < 1 || py >= Main.maxTilesY - 1) continue;
			Tile h = Main.tile[px, py];
			if (!SealSolid(h)) continue;
			ushort type = h.TileType;
			if (TileID.Sets.Grass[type]) return type is TileID.JungleGrass or TileID.MushroomGrass ? TileID.Mud : TileID.Dirt;
			if (type < allowed.Length && allowed[type] && !TileID.Sets.Ore[type] && !Main.tileSand[type] &&
				!TileID.Sets.Falling[type] && !Main.tileFrameImportant[type]) return type;
		}
		return y < Main.worldSurface ? TileID.Dirt : TileID.Stone;
	}
}
