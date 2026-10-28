using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;

internal static class ResidenceReservationDiagnostics
{
	private static Rectangle[] regions = [];
	internal static void Capture(Mod mod)
	{
		regions = [];
		try
		{
			var field = typeof(StructureMap).GetField("_protectedStructures", BindingFlags.NonPublic | BindingFlags.Instance);
			if (field?.GetValue(GenVars.structures) is not List<Rectangle> rectangles)
				throw new MissingFieldException("StructureMap._protectedStructures");
			// Called synchronously before residence planning, after natural passes.
			regions = rectangles.ToArray();
			string path = Main.ActiveWorldFileData.Path + ".residence-reservations.json";
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonSerializer.Serialize(new {
				stage = "before-authored-residences", seed = Main.ActiveWorldFileData.Seed,
				width = Main.maxTilesX, height = Main.maxTilesY,
				artifact = mod.Code.ManifestModule.ModuleVersionId,
				worldTilesIncluded = false,
				derivedPyramidGuard = Worldgen.NativePyramidProtection.SnapshotProvenance,
				regions = regions.Select(r => new { r.X, r.Y, r.Width, r.Height })
			}));
			mod.Logger.Info($"DINGDONG_RESERVATION_SNAPSHOT|regions={regions.Length}|path={path}|tilesIncluded=False");
			regions = []; // RamOpt 28.10: written to the JSON above; nothing reads the copy later.
		}
		catch(Exception ex) { mod.Logger.Warn("Reservation diagnostics unavailable: " + ex.Message); }
	}

	internal static string Describe(Rectangle area)
	{
		// Replays restore a separate StructureMap; the generation-only captured
		// array may be empty or refer to a different map. Inspect the map that
		// actually decided admission, never report that stale array as no conflict.
		var field = typeof(StructureMap).GetField("_protectedStructures", BindingFlags.NonPublic | BindingFlags.Instance);
		if (field?.GetValue(GenVars.structures) is not List<Rectangle> current)
			return "diagnostic-map-unavailable";
		return string.Join(",", current.Where(r => r.Intersects(area)).Take(3).Select(r => r.ToString()));
	}

	internal static bool RunRequestedPreflight(Mod mod)
	{
		string keys = Environment.GetEnvironmentVariable("DINGDONG_RESIDENCE_PREFLIGHT_NPC");
		if (!Main.dedServ || string.IsNullOrWhiteSpace(keys) ||
			!Path.GetFileName(Main.ActiveWorldFileData?.Path ?? "").StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal)) return false;
		foreach (string key in keys.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			var profile = AuthoredCandidateDeploymentManifest.All.Single(p => p.NpcKey == key);
			CandidateDeploymentSitePlanner.DiagnoseTerrain(mod, profile, evaluateReservations: true);
		}
		Console.WriteLine("DINGDONG_RESIDENCE_PREFLIGHT_END|placementSkipped=True|diagnosticOnly=True");
		return true;
	}
}
