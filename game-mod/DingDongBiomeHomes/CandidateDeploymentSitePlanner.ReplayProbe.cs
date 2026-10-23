using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	internal static void ReplayReservationPlan(Mod mod, bool place = false, bool domainsOnly = false)
	{
		if (place && domainsOnly) throw new ArgumentException("Domain diagnostics cannot place residences.");
		if (place && Environment.GetEnvironmentVariable("DINGDONG_REPLAY_DERIVED_PYRAMID") == "1")
			throw new InvalidOperationException("Placement replay cannot mix current paired provenance with legacy guard migration.");
		string path = Main.ActiveWorldFileData.Path + ".residence-reservations.json";
		byte[] snapshotBytes = File.ReadAllBytes(path);
		using var document = JsonDocument.Parse(snapshotBytes);
		var root = document.RootElement;
		if (root.GetProperty("seed").GetInt32() != Main.ActiveWorldFileData.Seed ||
			root.GetProperty("width").GetInt32() != Main.maxTilesX || root.GetProperty("height").GetInt32() != Main.maxTilesY)
			throw new InvalidOperationException("Reservation snapshot does not match the loaded world.");
		StructureMap original = GenVars.structures;
		try
		{
			GenVars.structures = new StructureMap();
			int count = 0;
			List<Rectangle> restoredRegions = new();
			foreach(var r in root.GetProperty("regions").EnumerateArray())
			{
				Rectangle region = new(r.GetProperty("X").GetInt32(), r.GetProperty("Y").GetInt32(),
					r.GetProperty("Width").GetInt32(), r.GetProperty("Height").GetInt32());
				restoredRegions.Add(region);
			}
			restoredRegions = UpgradeVerifiedDerivedPyramid(mod, path, root, restoredRegions);
			foreach (Rectangle region in restoredRegions) GenVars.structures.AddProtectedStructure(region, 0);
			count = restoredRegions.Count;
			mod.Logger.Info($"DINGDONG_RESIDENCE_REPLAY_BEGIN|reservations={count}|tileStage=post-save-and-load|originalReservationStage=before-residences");
			if (domainsOnly)
			{
				ExportCandidateDomains(mod, restoredRegions);
				return;
			}
			if (place)
			{
				using var pyramidCapture = Worldgen.NativePyramidProtection.CaptureVerifiedPlacementReplay(
					mod, root, snapshotBytes, restoredRegions);
				ResidencePlacementReplay.Run(mod);
				return;
			}
			bool success = TryPlanSimultaneous(mod, AuthoredCandidateDeploymentManifest.All,
				out var sites, out string failure);
			mod.Logger.Info($"DINGDONG_RESIDENCE_REPLAY_END|verdict={(success ? "PASS" : "FAIL")}|sites={sites.Count}/61|failure={failure}|trace={LastSolverTrace}|worldMutated=False|housesPlaced=0");
		}
		finally { GenVars.structures = original; }
	}
}
