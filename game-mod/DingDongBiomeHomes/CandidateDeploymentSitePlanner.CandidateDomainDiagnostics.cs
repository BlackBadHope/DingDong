using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed class CandidateDomainAudit
	{
		private readonly IReadOnlyList<Rectangle> foreign;
		internal int BucketWidth { get; } = Math.Max(100, (Main.maxTilesX + 63) / 64);
		internal Dictionary<string, CandidateDomainProfileStatistics> Profiles { get; } = new(StringComparer.Ordinal);
		internal CandidateDomainAudit(IReadOnlyList<AuthoredDeploymentProfile> profiles, IReadOnlyList<Rectangle> regions)
		{
			foreign = regions;
			foreach (var profile in profiles) Profiles.Add(profile.NpcKey, new CandidateDomainProfileStatistics());
		}
		internal void Column(string npc, int x)
		{
			var p = Profiles[npc];
			p.CurrentX = x;
			Increment(p.Counts, "column-visits");
			Increment(Bucket(p, x).Counts, "column-visits");
		}
		internal void Rejection(string npc, string reason, string detail)
		{
			var p = Profiles[npc];
			Increment(p.Counts, reason);
			var bucket = Bucket(p, p.CurrentX);
			Increment(bucket.Counts, reason);
			bucket.Examples.TryAdd(reason, detail);
		}
		internal void Admission(CandidateDeploymentSite candidate, bool admitted, string reason)
		{
			var p = Profiles[candidate.NpcKey];
			int x = candidate.AssetArea.X;
			string normalized = admitted ? "admission:accepted" : "admission:" + reason.Split(':')[0];
			Increment(p.Counts, normalized);
			var bucket = Bucket(p, x);
			Increment(bucket.Counts, normalized);
			if (admitted || !bucket.Examples.TryAdd(normalized, reason) || p.AdmissionExamples.Count >= 256) return;
			var profile = AuthoredCandidateDeploymentManifest.All.Single(v => v.NpcKey == candidate.NpcKey);
			Rectangle actual = Worldgen.Compiler.Runtime.WorldCompilerRuntimeState.Current == null
				? Worldgen.ResidenceWriteSafety.Bounds(candidate.ProtectedArea, profile.ProtectedPadding) : candidate.ProtectedArea;
			List<object> conflicts = new();
			int intersections = 0;
			for (int id = 0; id < foreign.Count; id++)
				if (actual.Intersects(foreign[id]))
				{
					intersections++;
					if (conflicts.Count < 8) conflicts.Add(new { id, area = DomainRectangle(foreign[id]) });
				}
			p.AdmissionExamples.Add(new {
				reason, asset = DomainRectangle(candidate.AssetArea), protect = DomainRectangle(candidate.ProtectedArea),
				actualAdmissionArea = DomainRectangle(actual), intersections, foreign = conflicts
			});
		}
		private CandidateDomainBucket Bucket(CandidateDomainProfileStatistics p, int x)
		{
			int start = x / BucketWidth * BucketWidth;
			if (!p.XBuckets.TryGetValue(start, out var bucket))
				p.XBuckets.Add(start, bucket = new CandidateDomainBucket());
			return bucket;
		}
		internal static void Increment(Dictionary<string, long> target, string reason, long count = 1)
		{
			target.TryGetValue(reason, out long old);
			target[reason] = old + count;
		}
	}
	private sealed class CandidateDomainProfileStatistics
	{
		internal int CurrentX;
		public Dictionary<string, long> Counts { get; } = new(StringComparer.Ordinal);
		public Dictionary<string, long> SurfaceSummary { get; } = new(StringComparer.Ordinal);
		public SortedDictionary<int, CandidateDomainBucket> XBuckets { get; } = new();
		public List<object> AdmissionExamples { get; } = new();
		public List<object> HorizontalRanges { get; } = new();
		public Dictionary<string, string> TerrainExamples { get; } = new(StringComparer.Ordinal);
	}
	private sealed class CandidateDomainBucket
	{
		public Dictionary<string, long> Counts { get; } = new(StringComparer.Ordinal);
		public Dictionary<string, string> Examples { get; } = new(StringComparer.Ordinal);
	}
	private sealed partial class SurfacePlanningSearch
	{
		internal void CaptureCandidateDomainDiagnostics()
		{
			if (activeCandidateDomainAudit == null) return;
			var p = activeCandidateDomainAudit.Profiles[profile.NpcKey];
			if (p.HorizontalRanges.Count == 0)
				foreach (var range in ranges) p.HorizontalRanges.Add(new { min = range.Min, max = range.Max });
			var counters = new Dictionary<string, int> {
				["biome"] = biomeRejected, ["bounds"] = boundsRejected, ["reserved"] = reservedRejected,
				["persistent-candidates"] = persistentCandidates, ["unsupported-persistent"] = unsupportedPersistentCandidates,
				["critical-structure"] = protectedStructureRejected, ["terrain"] = terrainRejected,
				["spacing"] = spacingRejected, ["independent"] = independentRejected, ["mega"] = megaRejected,
				["density"] = densityRejected, ["mass"] = massRejected, ["landmark"] = landmarkRejected
			};
			foreach (var pair in counters) CandidateDomainAudit.Increment(p.SurfaceSummary, pair.Key, pair.Value);
			foreach (var pair in unsupportedPersistentReasons) CandidateDomainAudit.Increment(p.SurfaceSummary, "persistent:" + pair.Key, pair.Value);
			foreach (var pair in terrainReasons) CandidateDomainAudit.Increment(p.SurfaceSummary, "terrain:" + pair.Key, pair.Value);
			foreach (var pair in terrainExamples) p.TerrainExamples.TryAdd(pair.Key, pair.Value);
		}
	}
}
