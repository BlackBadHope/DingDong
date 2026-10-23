using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private readonly IReadOnlyList<CandidateDeploymentSite> sites;
        private readonly IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks;
        private readonly bool requireFullLayerPopulation;
        private readonly int maximumLayerCoverageViolations;
        private readonly int minimumDistinctSkyBands;
        private readonly bool enforceAestheticDistribution;
        private readonly int maximumVisualSpacingViolations;
        private readonly bool allowDisjointFocusOverlap;
		private readonly bool enforceOceanBalance;

        private readonly int[] skyTops = new int[8];
        private readonly HashSet<int> skyBuckets = new();
        private readonly int[,] counts = new int[MacroSectorCount, 4];
        private readonly int[,] masses = new int[MacroSectorCount, 4];
        private readonly int[,] megas = new int[MacroSectorCount, 4];
        private readonly int[,] landmarks = new int[MacroSectorCount, 4];
        private readonly int[,] isolations = new int[MacroSectorCount, 4];
        private readonly HashSet<ResidenceAltitudeBand> skyBands = new();
        private readonly HashSet<ResidenceAltitudeBand> cavernBands = new();
        private readonly int[] skyBandCounts = new int[3];
        private readonly int[] layerTotals = new int[4];
        private readonly int[] layerSectors = new int[4];
        private readonly int[] layerLeft = new int[4];
        private readonly int[] layerRight = new int[4];

        private int skyCount;
        private int distinctSky;
        private int leftOcean;
        private int rightOcean;
        private int ocean;
        private int spacing;
        private int megaCell;
        private int landmarkAdj;
        private int independent;
        private int massFail;
        private int siteDensity;
        private int altitudeViolations;
        private int surfaceDepthViolations;
        private int isolatedEnvelope;
        private int landmarkMegaCell;
        private int landmarkMegaStack;
        private int highSkyCeiling;
        private int maximumSurfaceDepth = int.MinValue;
        private int minimumSurfaceDepth = int.MaxValue;
        private int maximumSurfaceRelief;
        private int maximumSkyBandCount;
        private int layerCoverageViolations;
        private bool skyOk;
        private bool oceanOk;
        private bool layerPopulationOk;

        public LayoutAcceptanceSession(
            IReadOnlyList<CandidateDeploymentSite> sites,
            IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks,
            bool requireFullLayerPopulation,
            int maximumLayerCoverageViolations,
            int minimumDistinctSkyBands,
            bool enforceAestheticDistribution,
            int maximumVisualSpacingViolations,
            bool allowDisjointFocusOverlap,
			bool enforceOceanBalance)
        {
            this.sites = sites;
            this.masks = masks;
            this.requireFullLayerPopulation = requireFullLayerPopulation;
            this.maximumLayerCoverageViolations = maximumLayerCoverageViolations;
            this.minimumDistinctSkyBands = minimumDistinctSkyBands;
            this.enforceAestheticDistribution = enforceAestheticDistribution;
            this.maximumVisualSpacingViolations = maximumVisualSpacingViolations;
            this.allowDisjointFocusOverlap = allowDisjointFocusOverlap;
			this.enforceOceanBalance = enforceOceanBalance;
        }
    }
}
