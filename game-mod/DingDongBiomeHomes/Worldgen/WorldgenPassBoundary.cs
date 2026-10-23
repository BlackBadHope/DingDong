using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

internal static class WorldgenPassBoundary
{
	// Move only our final consumer. Validate before any change; preserve all
	// foreign pass objects and their relative order, including disabled passes.
	internal static bool MoveOwnedLast<T>(IList<T> passes, T owned,
		Func<T, string> name, string ownedName) where T : class
	{
		int index = -1, identities = 0, names = 0, cleanup = 0;
		for (int i = 0; i < passes.Count; i++)
		{
			if (ReferenceEquals(passes[i], owned)) { index = i; identities++; }
			if (name(passes[i]) == ownedName) names++;
			if (name(passes[i]) == "Final Cleanup") cleanup++;
		}
		if (identities != 1 || names != 1 || cleanup != 1 || name(owned) != ownedName)
			throw new InvalidOperationException("Unsupported authored residence pass boundary: " +
				$"identity={identities}, names={names}, cleanup={cleanup}.");
		if (index == passes.Count - 1) return false;
		passes.RemoveAt(index);
		passes.Add(owned);
		return true;
	}
}
