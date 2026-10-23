using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private string FailureSummary() =>
			$"{BaseSummary()}|{PairSupportSummary()}|sectorCoverageForwardRejected={coverageForwardRejectedBranches}|sectorLeafOceanRejected={leafOceanRejectedPlans}|sectorLeafCoverageRejected={leafCoverageRejectedPlans}|sectorMacChecks={macChecks}|sectorMacRejectedBranches={macRejectedBranches}|sectorArcPrepassEnabled={ConstraintSectorArcPairEvaluationLimit > 0}|sectorArcPlans={arcPlans}|sectorArcRejectedPlans={arcRejectedPlans}|sectorDownstreamRejectedPlans={downstreamRejectedPlans}|sectorDownstreamAdmissionBudgetExhausted={downstreamAdmissionBudgetExhausted}|sectorArcPairEvaluations={arcPairEvaluations}|sectorArcRevisions={arcRevisions}|sectorArcRemoved={arcValuesRemoved}|sectorArcBudgetExhausted={arcBudgetExhausted}|sectorArcWipeoutWitness={arcWipeoutWitness}";

		private string SuccessSummary(Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> restrictedPools)
		{
			int restrictedTotal = 0;
			int restrictedMinimum = int.MaxValue;
			int restrictedMaximum = 0;
			foreach (string variable in variables)
			{
				IReadOnlyList<RankedLayoutCandidate> restricted = restrictedPools[variable];
				restrictedTotal += restricted.Count;
				restrictedMinimum = Math.Min(restrictedMinimum, restricted.Count);
				restrictedMaximum = Math.Max(restrictedMaximum, restricted.Count);
			}

			FindPeakLoads(out int peakMass, out int peakIsolation, out int peakMega);
			return $"{BaseSummary()}|sectorPeakMass={peakMass}/{AuthoredResidenceLayoutPolicy.MaxSectorMass}|sectorPeakIsolation={peakIsolation}/layer-policy|sectorPeakMega={peakMega}/{AuthoredResidenceLayoutPolicy.MaxMegaPerSectorLayer}|sectorOcean={oceanLeft}/{oceanRight}|{PairSupportSummary()}|sectorCoverageForwardRejected={coverageForwardRejectedBranches}|sectorLeafOceanRejected={leafOceanRejectedPlans}|sectorLeafCoverageRejected={leafCoverageRejectedPlans}|sectorMacChecks={macChecks}|sectorMacRejectedBranches={macRejectedBranches}|sectorArcPrepassEnabled={ConstraintSectorArcPairEvaluationLimit > 0}|sectorArcPlans={arcPlans}|sectorArcRejectedPlans={arcRejectedPlans}|sectorDownstreamRejectedPlans={downstreamRejectedPlans}|sectorDownstreamAdmissionBudgetExhausted={downstreamAdmissionBudgetExhausted}|sectorArcPairEvaluations={arcPairEvaluations}|sectorArcRevisions={arcRevisions}|sectorArcRemoved={arcValuesRemoved}|sectorArcBudgetExhausted={arcBudgetExhausted}|sectorArcWipeoutWitness={arcWipeoutWitness}|restrictedTotal={restrictedTotal}|restrictedMin={restrictedMinimum}|restrictedMax={restrictedMaximum}";
		}

		private string BaseSummary() =>
			$"sectorItems={items.Count}/{variables.Count}|sectorOptions={optionTotal}|sectorOptionMin={optionMinimum}|sectorOptionMax={optionMaximum}|sectorNodes={nodes}|sectorBacktracks={backtracks}|sectorDeadDomains={deadDomains}|sectorMaxAssigned={maxAssigned}/{items.Count}|sectorBudgetExhausted={budgetExhausted}|sectorOceanSupport={oceanSupportsLeft}/{oceanSupportsRight}|sectorOceanBilateralRequired={RequiresBilateralOcean}";

		private string PairSupportSummary() =>
			$"sectorPairQueries={sectorPairSupportQueries}|sectorPairCache={sectorPairSupportCache.Count}|sectorPairCacheHits={sectorPairSupportCacheHits}|sectorPairCandidateEvaluations={sectorPairSupportCandidateEvaluations}|sectorPairRejectedOptions={sectorPairRejectedOptions}|sectorPairBudgetExhausted={sectorPairSupportBudgetExhausted}";

		private void FindPeakLoads(out int peakMass, out int peakIsolation, out int peakMega)
		{
			peakMass = 0;
			peakIsolation = 0;
			peakMega = 0;
			for (int sector = 0; sector < AuthoredResidenceLayoutPolicy.MacroSectorCount; sector++)
			{
				for (int layer = 0; layer < 4; layer++)
				{
					peakMass = Math.Max(peakMass, masses[sector, layer]);
					peakIsolation = Math.Max(peakIsolation, isolations[sector, layer]);
					peakMega = Math.Max(peakMega, megas[sector, layer]);
				}
			}
		}
	}
}
