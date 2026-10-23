using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private readonly Dictionary<string, int> terrainReasons = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string> terrainExamples = new(StringComparer.Ordinal);

		private bool RejectTerrain(string reason, string detail)
		{
			activeCandidateDomainAudit?.Rejection(profile.NpcKey, "terrain:" + reason, detail);
			terrainRejected++;
			terrainReasons.TryGetValue(reason, out int count);
			terrainReasons[reason] = count + 1;
			// One example per reason, not one log line per candidate.
			terrainExamples.TryAdd(reason, detail);
			return false;
		}

		private string TerrainDiagnostics => string.Join(";", terrainReasons
			.OrderBy(pair => pair.Key, StringComparer.Ordinal)
			.Select(pair => $"{pair.Key}x{pair.Value}[{terrainExamples[pair.Key]}]"));
	}
}
