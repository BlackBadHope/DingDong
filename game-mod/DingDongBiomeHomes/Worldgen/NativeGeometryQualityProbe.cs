using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Isolated dedicated fixture only, dispatched after native load/settling.
// Exports evidence, not owner approval, simulated gameplay or repairs.
internal static class NativeGeometryQualityProbe
{
	internal static void Run(Mod mod)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Quality probe requires an isolated fixture.");
		// A failed house must not suppress the evidence needed to diagnose it.
		Exception acceptanceFailure = null;
		try { ResidenceAcceptanceProbe.Run(mod); }
		catch (InvalidOperationException ex) { acceptanceFailure = ex; }
		DingDongAuthoredStructureLoader.ExportSceneAudit(mod);
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "GeometryQuality", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
		Directory.CreateDirectory(directory);
		int top = Math.Max(0, (int)Main.worldSurface - 800);
		NativeTerrainRaster.ExportRegion(Path.Combine(directory, "coast-left.bmp"), new(0, top, 2400, 1800));
		NativeTerrainRaster.ExportRegion(Path.Combine(directory, "coast-right.bmp"), new(Main.maxTilesX - 2400, top, 2400, 1800));
		NativeTerrainRaster.ExportRegion(Path.Combine(directory, "underworld.bmp"), new(Main.maxTilesX / 2 - 900, Main.maxTilesY - 1000, 1800, 1000));
		NativeTerrainRaster.ExportRegion(Path.Combine(directory, "underworld-full-width.bmp"), new(0, Main.maxTilesY - 1000, Main.maxTilesX, 1000), 4);
		var targets = new Dictionary<int, string>();
		for (int type = 0; type < TileLoader.TileCount; type++)
		{
			string name = TileLoader.GetTile(type)?.FullName;
			if (name is "SOTS/PyramidGateTile" or "ThoriumMod/MarkedGraveTileTrue") targets.Add(type, name);
		}
		var bounds = new Dictionary<string, Rectangle>();
		int scanBottom = Math.Min(Main.maxTilesY, (int)Main.worldSurface + 1800);
		for (int x = 0; x < Main.maxTilesX; x++)
		for (int y = 0; y < scanBottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile || !targets.TryGetValue(tile.TileType, out string name)) continue;
			Rectangle cell = new(x, y, 1, 1);
			bounds[name] = bounds.TryGetValue(name, out Rectangle prior) ? Rectangle.Union(prior, cell) : cell;
		}
		var landmarks = new List<object>();
		foreach (var (name, area) in bounds)
		{
			Rectangle context = new(area.Center.X - 600, area.Center.Y - 500, 1200, 1000);
			NativeTerrainRaster.ExportRegion(Path.Combine(directory, name.Replace('/', '-') + ".bmp"), context);
			landmarks.Add(new { name, x = area.X, y = area.Y, width = area.Width, height = area.Height });
		}
		var airBands = new List<object>();
		for (int bottomOffset = 100; bottomOffset <= 900; bottomOffset += 100)
		{
			int open = 0, samples = 0;
			for (int x = 100; x < Main.maxTilesX - 100; x += 40)
			{
				Tile tile = Main.tile[x, Main.maxTilesY - bottomOffset];
				if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) open++;
				samples++;
			}
			airBands.Add(new { bottomOffset, open, samples });
		}
		var densityProfile = new List<object>();
		for (int offset = 20; offset <= 950; offset += 5)
		for (int sector = 0; sector < 8; sector++)
		{
			int samples = 0, solid = 0, liquid = 0, run = 0, longestOpenRun = 0;
			int left = Math.Max(100, Main.maxTilesX * sector / 8);
			int right = Math.Min(Main.maxTilesX - 100, Main.maxTilesX * (sector + 1) / 8);
			for (int x = left; x < right; x += 40)
			{
				Tile tile = Main.tile[x, Main.maxTilesY - offset];
				bool blocked = tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
				if (blocked) solid++;
				if (tile.LiquidAmount > 0) liquid++;
				run = blocked ? 0 : run + 1;
				longestOpenRun = Math.Max(longestOpenRun, run);
				samples++;
			}
			densityProfile.Add(new { offset, sector, samples, solid, liquid, longestOpenRun });
		}
		int policyLayer = NativeUnderworldSystem.Layer;
		int directGetterLayer = Main.UnderworldLayer;
		int reflectedGetterLayer = (int)typeof(Main).GetProperty(nameof(Main.UnderworldLayer)).GetValue(null);
		File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new {
			worldPath = Main.ActiveWorldFileData.Path, seed = Main.ActiveWorldFileData.Seed,
			width = Main.maxTilesX, height = Main.maxTilesY,
			geometryVersion = NativeUnderworldSystem.GeometryVersion, policyLayer, directGetterLayer, reflectedGetterLayer,
			expandedUnderworldActive = NativeUnderworldSystem.Active,
			worldSurface = Main.worldSurface, landmarks, airBands, densityProfile,
			densitySampling = "actual tiles every 40 X and 5 Y, 8 horizontal sectors; not a complete tile census",
			overviewSampling = "one actual tile per 4x4 block; 1:1 central crop retained; mod colors are diagnostic",
			geometryVerdict = "UNASSESSED: open space alone is not a defect; compare profile and terrain with original complaint",
			probeMutatedWorld = false, nativeLoadSettlingCompleted = true, gameplayTicksRequired = false,
			worldSaved = false, visualApproval = false, acceptanceFailure = acceptanceFailure?.Message
		}, new JsonSerializerOptions { WriteIndented = true }));
		if (Environment.GetEnvironmentVariable("DINGDONG_BIG_GENERATION_PROBE") == "1")
			NativeTerrainRaster.Export();
		NativeBigRcBoundaryAudit.Run(mod);
		if (!NativeSanctuaryPlacementSystem.ValidateGeometry(mod))
			throw new InvalidOperationException("Sanctuary placement disagrees with saved physical geometry.");
		mod.Logger.Info($"DINGDONG_GEOMETRY_QUALITY|state=EXPORTED|geometryVersion={NativeUnderworldSystem.GeometryVersion}|policyLayer={policyLayer}|directGetterLayer={directGetterLayer}|reflectedGetterLayer={reflectedGetterLayer}|afterNativeLoadSettling=True|directory={directory}|probeMutatedWorld=False|visualApproval=False");
		if (acceptanceFailure != null) throw new InvalidOperationException("Post-settling acceptance failed; diagnostic exports retained.", acceptanceFailure);
	}
}
