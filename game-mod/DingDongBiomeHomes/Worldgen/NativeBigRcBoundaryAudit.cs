using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Actual tile evidence only: a nonzero boundary count is not an acceptance failure.
internal static class NativeBigRcBoundaryAudit
{
	internal static void Run(Mod mod)
	{
		string worldPath = Main.ActiveWorldFileData?.Path;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_BIG_GENERATION_PROBE") != "1" ||
			!Path.GetFileName(worldPath ?? string.Empty).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal) ||
			!ContinentalWorldDimensions.IsContinental) return;

		if (!ModLoader.TryGetMod("CalamityMod", out Mod provider))
			throw new InvalidOperationException("Abyss boundary audit requires CalamityMod.");
		Type abyss = provider.Code.GetType("CalamityMod.World.Abyss", true);
		const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		FieldInfo sideField = abyss.GetField("AtLeftSideOfWorld", flags);
		FieldInfo bottomField = abyss.GetField("AbyssChasmBottom", flags);
		if (sideField?.FieldType != typeof(bool) || bottomField?.FieldType != typeof(int))
			throw new InvalidOperationException("Calamity Abyss boundary field contract unavailable.");
		bool left = (bool)sideField.GetValue(null);
		int bottom = (int)bottomField.GetValue(null);
		var tileNames = new Dictionary<ushort, string>();
		foreach (string name in new[] { "SulphurousShale", "AbyssGravel", "PyreMantle", "Voidstone" })
		{
			if (!provider.TryFind<ModTile>(name, out var tile))
				throw new InvalidOperationException("Calamity Abyss tile unavailable: " + name);
			tileNames.Add(checked((ushort)tile.Type), name);
		}
		if (tileNames.Count != 4) throw new InvalidOperationException("Calamity Abyss layer tile IDs unavailable.");

		int layer = NativeUnderworldSystem.Layer;
		int width = Math.Min(1400, Main.maxTilesX);
		int xStart = left ? 0 : Main.maxTilesX - width;
		int rasterTop = Math.Max(0, layer - 500);
		int rasterBottom = Math.Min(Main.maxTilesY, layer + 351);
		Rectangle area = new(xStart, rasterTop, width, rasterBottom - rasterTop);
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "AbyssBoundary",
			DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
		Directory.CreateDirectory(directory);
		MapHelper.Initialize(); // Initializes the diagnostic palette; does not edit world tiles.
		NativeTerrainRaster.ExportRegion(Path.Combine(directory, "boundary.bmp"), area, 1);

		int rowTop = Math.Max(0, layer - 200);
		int rowBottom = Math.Min(Main.maxTilesY, layer + 201);
		int aggregateTop = Math.Clamp(layer, 0, Main.maxTilesY);
		var rows = new List<object>();
		var totals = new TileCounts(tileNames);
		for (int y = Math.Min(rowTop, aggregateTop); y < Main.maxTilesY; y++)
		{
			var counts = new TileCounts(tileNames);
			for (int x = xStart; x < xStart + width; x++) counts.Observe(Main.tile[x, y], tileNames);
			if (y >= rowTop && y < rowBottom) rows.Add(new { y, offsetFromLayer = y - layer, counts });
			if (y >= aggregateTop) totals.Add(counts);
		}
		var resolvedTiles = new List<object>();
		foreach (var (type, name) in tileNames) resolvedTiles.Add(new { name, type });
		File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new {
			worldPath, width = Main.maxTilesX, height = Main.maxTilesY, layer,
			side = left ? "left" : "right", bottomField = bottom,
			calamityVersion = provider.Version.ToString(), resolvedTiles,
			raster = new { path = "boundary.bmp", x = area.X, y = area.Y, width = area.Width, height = area.Height,
				rightExclusive = area.Right, bottomExclusive = area.Bottom, step = 1 },
			rowCensus = new { left = xStart, rightExclusive = xStart + width, top = rowTop, bottomExclusive = rowBottom, rows },
			atOrBelowUnderworldLayer = new { left = xStart, rightExclusive = xStart + width,
				top = aggregateTop, bottomExclusive = Main.maxTilesY, counts = totals },
			counting = "Complete actual tile census in the stated edge ranges. Material counts require HasTile; solid counts also require non-actuated, tileSolid and not tileSolidTop. Open is samples minus solids; liquidSamples counts any positive LiquidAmount independently.",
			rasterMeaning = "One actual tile per pixel; diagnostic map palette, not in-game texture/frame/paint rendering. Vertical raster includes layer-500 through layer+350, clipped to the world.",
			verdict = "UNASSESSED",
			verdictReason = "Quantitative evidence for terrain review. Nonzero Abyss material or solid counts at/below the layer do not alone establish a defect; native decorative offsets may cross by a few tiles.",
			userSavesUsed = false, worldMutated = false, visualApproval = false
		}, new JsonSerializerOptions { WriteIndented = true }));
		mod.Logger.Info($"DINGDONG_ABYSS_BOUNDARY|state=EXPORTED|verdict=UNASSESSED|layer={layer}|side={(left ? "left" : "right")}|bottomField={bottom}|solidAbyssAtOrBelowLayer={totals.solidAbyssTiles}|directory={directory}|worldMutated=False|visualApproval=False");
	}

	private sealed class TileCounts
	{
		public long samples { get; private set; }
		public long solids { get; private set; }
		public long open => samples - solids;
		public long liquidSamples { get; private set; }
		public long solidAbyssTiles { get; private set; }
		public Dictionary<string, long> abyssTiles { get; } = new();
		public Dictionary<string, long> solidAbyssByType { get; } = new();

		internal TileCounts(Dictionary<ushort, string> tileNames)
		{
			foreach (string name in tileNames.Values) { abyssTiles.Add(name, 0); solidAbyssByType.Add(name, 0); }
		}

		internal void Observe(Tile tile, Dictionary<ushort, string> tileNames)
		{
			samples++;
			bool solid = tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
			if (solid) solids++;
			if (tile.LiquidAmount > 0) liquidSamples++;
			if (!tile.HasTile || !tileNames.TryGetValue(tile.TileType, out string name)) return;
			abyssTiles[name]++;
			if (solid) { solidAbyssTiles++; solidAbyssByType[name]++; }
		}

		internal void Add(TileCounts row)
		{
			samples += row.samples;
			solids += row.solids;
			liquidSamples += row.liquidSamples;
			solidAbyssTiles += row.solidAbyssTiles;
			foreach (var (name, count) in row.abyssTiles) abyssTiles[name] += count;
			foreach (var (name, count) in row.solidAbyssByType) solidAbyssByType[name] += count;
		}
	}
}
