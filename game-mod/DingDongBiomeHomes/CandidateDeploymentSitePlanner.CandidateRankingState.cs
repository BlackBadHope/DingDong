using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class RankedCandidateCollector
	{
		private readonly System.Func<CandidateDeploymentSite, bool> admission;
		private readonly int limit;
		private readonly int normalCandidatesPerBucket;
		private readonly int tightCandidatesPerBucket;
		private readonly int normalNeighborhood;
		private readonly int tightNeighborhood;
		public List<(CandidateDeploymentSite Site, int Score)> Items { get; } = new();

		private RankedCandidateCollector(int limit, int normalCandidatesPerBucket, int tightCandidatesPerBucket, int normalNeighborhood, int tightNeighborhood, System.Func<CandidateDeploymentSite, bool> admission = null)
		{
			this.admission = admission;
			this.limit = limit;
			this.normalCandidatesPerBucket = normalCandidatesPerBucket;
			this.tightCandidatesPerBucket = tightCandidatesPerBucket;
			this.normalNeighborhood = normalNeighborhood;
			this.tightNeighborhood = tightNeighborhood;
		}

		public static RankedCandidateCollector RuntimeBeam() => new(RankedCandidateLimit, RankedCandidatesPerMacroBucket, TightBiomeCandidatesPerMacroBucket, 96, 32);
		public static RankedCandidateCollector ConstraintPool(System.Func<CandidateDeploymentSite, bool> admission) => new(ConstraintCollectorLimit, ConstraintCollectorCandidatesPerMacroBucket, ConstraintCollectorCandidatesPerMacroBucket, 48, 16, admission);
	}
}
