using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AndroLibBagCoverage
{
	private static PropertyInfo bossBagsProperty;
	internal static int Roots { get; private set; }
	internal static int Expected { get; private set; }
	internal static bool Verified { get; private set; }

	internal static void Initialize(Mod provider)
	{
		Type data = provider.Code.GetType("androLib.Common.Utility.BossBagsData", true);
		bossBagsProperty = data.GetProperty("BossBags",
			BindingFlags.Static | BindingFlags.Public);
		MethodInfo getter = bossBagsProperty?.GetMethod;
		if (bossBagsProperty?.PropertyType != typeof(SortedSet<int>) || getter is null ||
			!getter.IsStatic || !getter.IsPublic || getter.GetParameters().Length != 0)
			throw Changed("BossBags getter");
		Roots = Expected = 0;
		Verified = false;
	}

	internal static void Verify(IReadOnlyDictionary<int, int> actual)
	{
		if (bossBagsProperty?.GetValue(null) is not SortedSet<int> expected ||
			expected.Count == 0)
			throw Changed("BossBags set unavailable");
		HashSet<int> nonMaterialized = AndroLibNonMaterializedBags.Verify(expected);
		int materializedCount = expected.Count - nonMaterialized.Count;
		int[] missing = expected.Where(itemType => !actual.ContainsKey(itemType)).ToArray();
		int[] extra = actual.Keys.Where(itemType => !expected.Contains(itemType)).ToArray();
		if (actual.Count != materializedCount ||
			!nonMaterialized.SetEquals(missing) || extra.Length != 0)
			throw Changed($"root set size actual={actual.Count} expected={materializedCount} " +
				$"missing=[{string.Join(",", missing)}] extra=[{string.Join(",", extra)}]");
		foreach (int itemType in expected.Where(itemType => !nonMaterialized.Contains(itemType)))
			if (!actual.TryGetValue(itemType, out int count) || count != 1)
				throw Changed($"item {itemType}");
		Roots = actual.Count;
		Expected = materializedCount;
		Verified = true;
	}

	internal static void Reset()
	{
		bossBagsProperty = null;
		Roots = Expected = 0;
		Verified = false;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"androLib bag coverage changed: {surface}.");
}
