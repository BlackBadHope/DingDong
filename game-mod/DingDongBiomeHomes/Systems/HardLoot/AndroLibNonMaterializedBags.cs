using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AndroLibNonMaterializedBags
{
	private static readonly int[] ExactIds =
	[
		ItemID.BossBagOgre,
		ItemID.BossBagDarkMage
	];

	internal static HashSet<int> Verify(SortedSet<int> providerBags)
	{
		if (ItemID.BossBagOgre != 3861 || ItemID.BossBagDarkMage != 3862)
			throw Changed("item ids");

		HashSet<int> deprecated = providerBags
			.Where(itemType => IsDeprecatedBossBag(itemType))
			.ToHashSet();
		if (!deprecated.SetEquals(ExactIds))
			throw Changed($"deprecated set=[{string.Join(",", deprecated)}]");

		return deprecated;
	}

	private static bool IsDeprecatedBossBag(int itemType)
	{
		if (itemType <= 0 || itemType >= ItemID.Sets.BossBag.Length ||
			itemType >= ItemID.Sets.Deprecated.Length)
			throw Changed($"item range {itemType}");
		return ItemID.Sets.BossBag[itemType] && ItemID.Sets.Deprecated[itemType];
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"androLib non-materialized bag contract changed: {surface}.");
}
