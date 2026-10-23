using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private static List<IReadOnlyList<string>> BuildConflictGroups(IReadOnlyList<(string Left, string Right)> conflicts, List<string> variables)
	{
		List<HashSet<string>> componentVariables = new();
		foreach ((string left, string right) in conflicts)
		{
			int leftComponent = -1;
			int rightComponent = -1;
			for (int index = 0; index < componentVariables.Count; index++)
			{
				if (componentVariables[index].Contains(left))
					leftComponent = index;
				if (componentVariables[index].Contains(right))
					rightComponent = index;
			}

			if (leftComponent < 0 && rightComponent < 0)
			{
				componentVariables.Add(new HashSet<string>(StringComparer.Ordinal) { left, right });
			}
			else if (leftComponent < 0)
			{
				componentVariables[rightComponent].Add(left);
			}
			else if (rightComponent < 0)
			{
				componentVariables[leftComponent].Add(right);
			}
			else if (leftComponent != rightComponent)
			{
				componentVariables[leftComponent].UnionWith(componentVariables[rightComponent]);
				componentVariables.RemoveAt(rightComponent);
			}
		}

		HashSet<string> portVariables = new(StringComparer.Ordinal);
		foreach (string variable in variables)
		{
			if (IsPortCohortVariable(variable))
				portVariables.Add(variable);
		}

		HashSet<string> combinedPortComponent = new(portVariables, StringComparer.Ordinal);
		bool hasPortConflict = false;
		foreach (HashSet<string> component in componentVariables)
		{
			if (!component.Overlaps(portVariables))
				continue;
			hasPortConflict = true;
			combinedPortComponent.UnionWith(component);
		}

		if (hasPortConflict && combinedPortComponent.Count <= ConstraintHardSafetyConflictGroupVariableLimit)
		{
			componentVariables.RemoveAll(component => component.Overlaps(portVariables));
			componentVariables.Add(combinedPortComponent);
		}

		componentVariables.Sort((left, right) =>
		{
			int count = left.Count.CompareTo(right.Count);
			if (count != 0)
				return count;
			string leftFirst = left.OrderBy(static value => value, StringComparer.Ordinal).First();
			string rightFirst = right.OrderBy(static value => value, StringComparer.Ordinal).First();
			return string.Compare(leftFirst, rightFirst, StringComparison.Ordinal);
		});
		List<IReadOnlyList<string>> groups = new(componentVariables.Count);
		foreach (HashSet<string> component in componentVariables)
		{
			List<string> group = new(component);
			group.Sort(StringComparer.Ordinal);
			groups.Add(group);
		}

		HashSet<string> combinedConflictVariables = new(StringComparer.Ordinal);
		foreach ((string left, string right) in conflicts)
		{
			combinedConflictVariables.Add(left);
			combinedConflictVariables.Add(right);
		}

		if (componentVariables.Count > 1 && combinedConflictVariables.Count <= ConstraintHardSafetyConflictGroupVariableLimit)
		{
			List<string> combined = new(combinedConflictVariables);
			combined.Sort(StringComparer.Ordinal);
			// Apparently independent conflict pairs can share fixed blockers. Solve their
			// endpoints together before trying each connected component in isolation.
			groups.Insert(0, combined);
		}

		return groups;
	}

	private static void AddSpatiallyDistinct(RankedLayoutCandidate candidate, List<RankedLayoutCandidate> balanced, HashSet<(int Side, int Bucket)> seenBuckets)
	{
		if (balanced.Count >= ConstraintHardSafetyPortCohortDomainLimit)
			return;
		int side = candidate.Site.AssetArea.Center.X < Main.maxTilesX / 2 ? 0 : 1;
		var bucket = (side, candidate.Site.AssetArea.Center.X / 16);
		if (seenBuckets.Add(bucket))
			balanced.Add(candidate);
	}
}
