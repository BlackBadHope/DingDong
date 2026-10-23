using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	// Read-only observer, scoped to one explicit diagnostic replay. Normal
	// generation and ordinary pool building have no observer and no extra scans.
	[ThreadStatic] private static CandidateDomainAudit activeCandidateDomainAudit;

	private static void ExportCandidateDomains(Mod mod, IReadOnlyList<Rectangle> restoredRegions)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") != "@candidate-domains" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Candidate domains require an explicit dedicated diagnostic world copy.");
		if (activeCandidateDomainAudit != null || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DINGDONG_RESIDENCE_POOL_REPLAY")))
			throw new InvalidOperationException("Candidate diagnostics require an unnested, unrestricted coordinate search.");
		string[] keys = (Environment.GetEnvironmentVariable("DINGDONG_DIAGNOSTIC_PROFILES") ?? "")
			.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
			throw new InvalidOperationException("Select 1 to 8 distinct NPC keys in DINGDONG_DIAGNOSTIC_PROFILES.");
		List<AuthoredDeploymentProfile> profiles = new();
		foreach (string key in keys)
		{
			if (!AuthoredCandidateDeploymentManifest.TryGet(key, out var profile))
				throw new InvalidOperationException("Unknown diagnostic profile: " + key);
			var layer = AuthoredResidencePlacementManifest.Required(key).BroadLayer;
			if (layer is not (BroadLayoutLayer.Surface or BroadLayoutLayer.Sky or BroadLayoutLayer.Underworld))
				throw new InvalidOperationException("This diagnostic covers Surface, Sky and Underworld profiles only: " + key);
			profiles.Add(profile);
		}
		if (!TryLoadPlanningAssets(mod, profiles, out var presets, out var masks, out string assetFailure))
			throw new InvalidOperationException(assetFailure);
		var index = SurfaceColumnIndex.Build();
		var audit = new CandidateDomainAudit(profiles, restoredRegions);
		Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools;
		bool nonempty;
		string summary, failure;
		activeCandidateDomainAudit = audit;
		try
		{
			// The exact runtime collector, ranking, pruning and admission gates;
			// deliberately no simultaneous solver, TryReserveAll, stamping or SaveWorld.
			nonempty = TryBuildConstraintCandidatePools(mod, profiles, presets, masks, index,
				out pools, out summary, out failure);
		}
		finally { activeCandidateDomainAudit = null; }
		CaptureDiagnosticPools(mod, pools);
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "CandidateDomains");
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json");
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			seed = Main.ActiveWorldFileData.Seed, width = Main.maxTilesX, height = Main.maxTilesY,
			purpose = "sampled runtime candidate domains and rejection causes; not a placement or satisfiability witness",
			worldMutated = false, housesPlaced = 0, solverRun = false, allSelectedPoolsNonempty = nonempty,
			summary, failure, xBucketWidth = audit.BucketWidth,
			counterScope = "visits across runtime salts; not unique candidate counts; admission is before ranking; surface-only counters remain empty for Underworld",
			foreignConflictScope = "exact intersections for first rejected example per reason and X bucket; capped at 256 examples/profile and 8 intersections/example",
			foreignReservations = restoredRegions.Select((r, id) => new { id, area = DomainRectangle(r) }),
			profiles = profiles.Select(p => new {
				npc = p.NpcKey, preset = p.PresetId, width = presets[p.NpcKey].Width, height = presets[p.NpcKey].Height,
				statistics = audit.Profiles[p.NpcKey],
				candidates = pools.TryGetValue(p.NpcKey, out var values) ? values.Select(c => new {
					asset = DomainRectangle(c.Site.AssetArea), protect = DomainRectangle(c.Site.ProtectedArea),
					biome = c.Site.Biome.ToString(), altitude = c.Site.AltitudeBand.ToString(), score = c.BaseScore
				}).ToArray() : []
			})
		}));
		mod.Logger.Info($"DINGDONG_CANDIDATE_DOMAINS|state=EXPORTED|profiles={profiles.Count}|pools={pools.Count}|candidates={pools.Values.Sum(p => p.Count)}|solverRun=False|worldMutated=False|path={path}");
	}

	private static object DomainRectangle(Rectangle r) => new { r.X, r.Y, r.Width, r.Height };
}
