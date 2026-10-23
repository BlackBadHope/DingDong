using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class WorldgenAdaptationProbe
{
	internal static void Run(Mod mod)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Adaptation probe requires an isolated diagnostic copy.");
		var retained = new Dictionary<string, (AuthoredSceneMutationMask Mask, string Digest)>();
		var tileZero = AuthoredTileSnapshot.Read(Main.tile[0, 0]);
		DingDongAuthoredStructureLoader.CacheUsage before;
		long liquidFlags = 0;
		using (DingDongAuthoredStructureLoader.UseTransientCaches())
		{
			foreach (var profile in AuthoredCandidateDeploymentManifest.All)
			{
				if (!NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset))
					throw new InvalidOperationException("Missing preset: " + profile.NpcKey);
				var mask = DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
				retained.Add(profile.NpcKey, (mask, Digest(mask)));
			}
			before = DingDongAuthoredStructureLoader.InspectCacheUsage();
			foreach (var profile in AuthoredCandidateDeploymentManifest.All)
			{
				NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset);
				liquidFlags += DingDongAuthoredStructureLoader.PrepareLiquidSafetySource(mod, preset, profile);
			}
			var withLiquid = DingDongAuthoredStructureLoader.InspectCacheUsage();
			if (liquidFlags <= 0 || withLiquid.PayloadBytes - before.PayloadBytes != liquidFlags)
				throw new InvalidOperationException("Liquid source cache is missing from payload accounting.");
			before = withLiquid;
			using (DingDongAuthoredStructureLoader.UseTransientCaches()) { }
			if (before != DingDongAuthoredStructureLoader.InspectCacheUsage())
				throw new InvalidOperationException("Nested consumer evicted live caches.");
		}
		if (retained.Count != 61 || before.PayloadBytes <= 0 ||
			DingDongAuthoredStructureLoader.InspectCacheUsage().PayloadBytes != 0)
			throw new InvalidOperationException("Transient cache did not release 61-profile pass payload.");
		DingDongAuthoredStructureLoader.ReportCacheRelease(mod, "adaptation-probe-first-release");
		try
		{
			using var cache = DingDongAuthoredStructureLoader.UseTransientCaches();
			foreach (var profile in AuthoredCandidateDeploymentManifest.All)
			{
				NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset);
				var fresh = DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
				var old = retained[profile.NpcKey];
				if (Digest(old.Mask) != old.Digest || Digest(fresh) != old.Digest)
					throw new InvalidOperationException("Eviction or reload changed scene mask: " + preset.Id);
			}
			throw new ProbeAbort();
		}
		catch (ProbeAbort) { }
		if (DingDongAuthoredStructureLoader.InspectCacheUsage().PayloadBytes != 0 ||
			tileZero != AuthoredTileSnapshot.Read(Main.tile[0, 0]))
			throw new InvalidOperationException("Exception cleanup or tile-zero invariant failed.");
		mod.Logger.Info($"DINGDONG_ADAPTATION_PROBE|verdict=PASS|profiles={retained.Count}" +
			$"|cachePayloadBytes={before.PayloadBytes}|liquidSourceFlagBytes={liquidFlags}|remainingBytes=0|maskDigestsEqual=True" +
			"|nestedLease=True|exceptionCleanup=True|worldMutated=False|generatedWorldClaim=False");
	}

	private sealed class ProbeAbort : Exception { }

	private static string Digest(AuthoredSceneMutationMask mask)
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		byte[] column = new byte[mask.Height];
		for (int x = 0; x < mask.Width; x++)
		{
			for (int y = 0; y < mask.Height; y++) column[y] = mask.MutatesLocal(x, y) ? (byte)1 : (byte)0;
			hash.AppendData(column);
		}
		return $"{mask.Width},{mask.Height},{mask.MutationCellCount},{mask.MutationBounds}," +
			$"{mask.TerrainAnchorY},{mask.FocusBounds},{mask.DiscardedMutationCellCount}:" + Convert.ToHexString(hash.GetHashAndReset());
	}
}
