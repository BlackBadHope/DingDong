using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		public bool TrySolve(out List<KeyValuePair<string, RankedLayoutCandidate>> componentSolution, out string componentTrace)
		{
			for (int layerIndex = 0; layerIndex < 4; layerIndex++)
			{
				BroadLayerWork work = CreateLayerWork((BroadLayoutLayer)layerIndex);
				if (work == null)
					continue;
				RunAestheticSolvers(work);
				if (!work.Solved)
					RunHardSafetySolvers(work);

				if (!work.Solved && work.CompleteSeed.Count != work.Variables.Count)
				{
					componentSolution = new List<KeyValuePair<string, RankedLayoutCandidate>>();
					componentTrace = string.Join(',', layerTraces);
					return false;
				}

				IReadOnlyList<KeyValuePair<string, RankedLayoutCandidate>> accepted = work.Solved ? work.Solution : work.CompleteSeed;
				if (!work.Solved)
					allLayersSolved = false;
				foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in accepted)
					merged.Add(assignment.Key, assignment.Value);
			}

			if (!allLayersSolved)
			{
				componentSolution = SolutionInVariableOrder();
				componentTrace = $"{string.Join(',', layerTraces)},merged={merged.Count}/{variables.Count},seed=complete-layer-best";
				return false;
			}
			return FinalizeMergedSolution(out componentSolution, out componentTrace);
		}

		private BroadLayerWork CreateLayerWork(BroadLayoutLayer layer)
		{
			List<string> layerVariables = new();
			Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains = new(StringComparer.Ordinal);
			int values = 0;
			foreach (string variable in variables)
			{
				if (AuthoredResidencePlacementManifest.Required(variable).BroadLayer != layer)
					continue;
				layerVariables.Add(variable);
				domains.Add(variable, unrestrictedPools[variable]);
				values += unrestrictedPools[variable].Count;
			}
			if (layerVariables.Count == 0)
				return null;
			layerVariables.Sort(static (left, right) =>
			{
				int compare = Rank(AuthoredResidencePlacementManifest.Required(left)).CompareTo(Rank(AuthoredResidencePlacementManifest.Required(right)));
				return compare != 0 ? compare : string.Compare(left, right, StringComparison.Ordinal);
			});
			return new BroadLayerWork
			{
				Layer = layer,
				Variables = layerVariables,
				Domains = domains,
				DomainValues = values,
				RepairBudget = layer == BroadLayoutLayer.Surface ? ConstraintSurfaceLayerRepairTimeLimitMilliseconds : ConstraintLayerRepairTimeLimitMilliseconds,
				ExactBudget = layer == BroadLayoutLayer.Surface ? ConstraintSurfaceLayerExactTimeLimitMilliseconds : ConstraintLayerExactTimeLimitMilliseconds,
				RepairRestarts = layer == BroadLayoutLayer.Surface ? ConstraintSurfaceRepairRestartLimit : ConstraintRepairRestartLimit,
				RepairSteps = layer == BroadLayoutLayer.Surface ? ConstraintSurfaceRepairStepsPerRestart : ConstraintRepairStepsPerRestart,
				Started = Environment.TickCount64
			};
		}
	}
}
