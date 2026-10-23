using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	public static bool TrySelfTest(out string failure)
	{
		string[] variables =
		{
			"wide",
			"scarce",
			"tail"
		};
		Dictionary<string, IReadOnlyList<int>> domains = new()
		{
			["wide"] = new[]
			{
				1,
				2
			},
			["scarce"] = new[]
			{
				1
			},
			["tail"] = new[]
			{
				2,
				3
			}
		};
		bool consistent = TryPrune(variables, domains, (_, candidate, _, other) => candidate != other, 100, out Dictionary<string, IReadOnlyList<int>> pruned, out ArcConsistencyPruneStats<string> stats);
		if (!consistent || stats.ValuesRemoved < 2 || pruned["wide"].Count != 1 || pruned["tail"].Count != 1)
		{
			failure = $"arc-prune:consistent={consistent}:removed={stats.ValuesRemoved}:wide={pruned["wide"].Count}:tail={pruned["tail"].Count}";
			return false;
		}

		Dictionary<string, IReadOnlyList<int>> impossible = new()
		{
			["left"] = new[]
			{
				1
			},
			["right"] = new[]
			{
				1
			}
		};
		bool impossibleConsistent = TryPrune(new[] { "left", "right" }, impossible, (_, candidate, _, other) => candidate != other, 10, out _, out ArcConsistencyPruneStats<string> impossibleStats);
		if (impossibleConsistent || !impossibleStats.Wipeout)
		{
			failure = $"arc-wipeout:consistent={impossibleConsistent}:wipeout={impossibleStats.Wipeout}";
			return false;
		}

		Dictionary<string, IReadOnlyList<int>> restricted = new()
		{
			["wide"] = new[]
			{
				2
			},
			["scarce"] = new[]
			{
				1
			},
			["tail"] = new[]
			{
				2,
				3
			}
		};
		bool maintained = TryPruneAfterDomainRestriction(variables, restricted, (_, candidate, _, other) => candidate != other, 100, new[] { "wide" }, out Dictionary<string, IReadOnlyList<int>> maintainedDomains, out ArcConsistencyPruneStats<string> maintainedStats);
		if (!maintained || maintainedStats.ValuesRemoved != 1 || maintainedDomains["tail"].Count != 1)
		{
			failure = $"arc-maintain:consistent={maintained}:removed={maintainedStats.ValuesRemoved}:tail={maintainedDomains["tail"].Count}";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
