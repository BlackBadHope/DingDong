using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static class MobRelicLegacyAliases
{
	internal const int ExpectedAliasCount = 40;
	internal static int RegisteredCount { get; private set; }

	private static readonly IReadOnlyDictionary<string, string[]> StableIdsByTarget =
		new Dictionary<string, string[]>(StringComparer.Ordinal)
		{
			["CalamityMod/Crabulon"] = ["538FEF1870DA2555", "E9DED7EA7FFDF105"],
			["SOTS/Glowmoth"] = ["26C14C9503C40138", "0179D7D7FC4F0CF7"],
			["ThoriumMod/TheGrandThunderBird"] = ["4337B51901FDFE2C", "60FACCD52D11C383"],
			["Terraria/EyeofCthulhu"] = ["8792C55F5B14F1FE", "148FB551DFAA7060"]
		};

	internal static void Register(IReadOnlyDictionary<string, MobRelicSet> sets)
	{
		if (RegisteredCount != 0)
			throw new InvalidOperationException("Mob relic legacy aliases were registered twice.");
		int total = 0;
		foreach ((string contentKey, string[] stableIds) in StableIdsByTarget)
		{
			if (!sets.TryGetValue(contentKey, out MobRelicSet set) || set.Items.Count != MobSpecificRelicCatalog.ItemsPerMob)
				throw new InvalidOperationException($"Legacy alias target is unavailable: {contentKey}.");
			for (int tier = 0; tier < set.Items.Count; tier++)
			{
				string[] names = Array.ConvertAll(stableIds, id => $"MobRelic_{id}_{tier}");
				ModTypeLookup<ModItem>.RegisterLegacyNames(set.Items[tier], names);
				total += names.Length;
			}
		}
		if (total != ExpectedAliasCount)
			throw new InvalidOperationException($"Mob relic legacy alias mismatch: {total}/{ExpectedAliasCount}.");
		RegisteredCount = total;
	}

	internal static void Reset() => RegisteredCount = 0;
}
