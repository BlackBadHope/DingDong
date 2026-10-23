using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private readonly IReadOnlyList<string> variables;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools;
		private readonly Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate, bool> pairCompatible;
		private readonly Func<IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>>, CapacityDomainAdmission> acceptRestrictedPools;
		private readonly List<CapacitySectorItem> items;
		private readonly Dictionary<(int ItemIndex, int Sector), IReadOnlyList<RankedLayoutCandidate>> candidatesByItemSector = new();
		private readonly int[,] counts = new int[AuthoredResidenceLayoutPolicy.MacroSectorCount, 4];
		private readonly int[,] masses = new int[AuthoredResidenceLayoutPolicy.MacroSectorCount, 4];
		private readonly int[,] isolations = new int[AuthoredResidenceLayoutPolicy.MacroSectorCount, 4];
		private readonly int[,] megas = new int[AuthoredResidenceLayoutPolicy.MacroSectorCount, 4];
		private readonly Dictionary<(int Sector, int Layer, HomeBiome Biome), int> landmarks = new();
		private readonly Dictionary<string, int> assignedSectors = new(StringComparer.Ordinal);
		private readonly bool[] assigned;
		private readonly Dictionary<(int LeftIndex, int LeftSector, int RightIndex, int RightSector), bool> sectorPairSupportCache = new();
		private int optionTotal;
		private int optionMinimum = int.MaxValue;
		private int optionMaximum;
		private int oceanTotal;
		private bool oceanSupportsLeft;
		private bool oceanSupportsRight;
		private int nodes;
		private int backtracks;
		private int deadDomains;
		private int maxAssigned;
		private int oceanLeft;
		private int oceanRight;
		private bool budgetExhausted;
		private bool downstreamAdmissionBudgetExhausted;
		private Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> acceptedRestrictedPools = new(StringComparer.Ordinal);
		private int arcPlans;
		private int arcRejectedPlans;
		private int downstreamRejectedPlans;
		private int arcPairEvaluations;
		private int arcRevisions;
		private int arcValuesRemoved;
		private bool arcBudgetExhausted;
		private string arcWipeoutWitness = "none";
		private int macChecks;
		private int macRejectedBranches;
		private int coverageForwardRejectedBranches;
		private int leafOceanRejectedPlans;
		private int leafCoverageRejectedPlans;
		private int sectorPairSupportQueries;
		private int sectorPairSupportCacheHits;
		private int sectorPairSupportCandidateEvaluations;
		private int sectorPairRejectedOptions;
		private bool sectorPairSupportBudgetExhausted;
		private Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains;
		private bool activeDomainsArcConsistent;

		private bool RequiresBilateralOcean =>
			oceanTotal >= 3 && oceanSupportsLeft && oceanSupportsRight;

		public CapacitySectorSearch(IReadOnlyList<string> variables, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools, Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate, bool> pairCompatible, Func<IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>>, CapacityDomainAdmission> acceptRestrictedPools)
		{
			this.variables = variables;
			this.pools = pools;
			this.pairCompatible = pairCompatible;
			this.acceptRestrictedPools = acceptRestrictedPools;
			items = new List<CapacitySectorItem>(variables.Count);
			assigned = new bool[variables.Count];
			activeDomains = new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(pools, StringComparer.Ordinal);
		}
	}
}
