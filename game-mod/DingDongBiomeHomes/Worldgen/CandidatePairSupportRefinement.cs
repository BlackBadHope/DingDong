using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Search control only: no world data, biome rules or placement predicates live here.
internal static class CandidatePairSupportRefinement
{
	internal enum Support { Witness, Unsupported, Unknown, Empty }

	internal static Support Check<T>(IReadOnlyList<T> left, IReadOnlyList<T> right,
		Func<T, T, bool> incompatible, BoundedRefinementBudget budget)
	{
		if (left.Count == 0 || right.Count == 0) return Support.Empty;
		foreach (T a in left)
			foreach (T b in right)
			{
				if (!budget.TryConsume()) return Support.Unknown;
				if (!incompatible(a, b)) return Support.Witness;
			}
		return Support.Unsupported;
	}

	// Old candidates (including their scores and order) must never be evicted by
	// refinement. The existing solver pool cap is unchanged.
	internal static List<T> AppendPreserving<T, TKey>(IReadOnlyList<T> existing,
		IEnumerable<T> additions, Func<T, TKey> key, int limit)
	{
		List<T> result = new(existing);
		HashSet<TKey> seen = new();
		foreach (T candidate in existing) seen.Add(key(candidate));
		foreach (T candidate in additions)
		{
			if (result.Count >= limit) break;
			if (seen.Add(key(candidate))) result.Add(candidate);
		}
		return result;
	}
}

internal sealed class BoundedRefinementBudget
{
	internal int Limit { get; }
	internal int Used { get; private set; }
	internal int Remaining => Limit - Used;
	internal BoundedRefinementBudget(int limit)
	{
		if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
		Limit = limit;
	}
	internal bool TryConsume()
	{
		if (Remaining == 0) return false;
		Used++;
		return true;
	}
}
