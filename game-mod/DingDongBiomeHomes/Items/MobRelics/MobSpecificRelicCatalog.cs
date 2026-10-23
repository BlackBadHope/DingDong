using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static class MobSpecificRelicCatalog
{
	internal const int ItemsPerMob = 5;
	private static readonly Dictionary<string, MobRelicSet> Sets = new(StringComparer.Ordinal);
	private static readonly List<GeneratedMobRelicItem> Items = new();

	internal static IReadOnlyDictionary<string, MobRelicSet> RegisteredSets => Sets;
	internal static IReadOnlyList<GeneratedMobRelicItem> RegisteredItems => Items;

	internal static void Register(Mod mod)
	{
		if (Sets.Count != 0 || Items.Count != 0)
			throw new InvalidOperationException("Mob-specific relic content was registered twice.");
		int mechanicOrdinal = 0;
		RegisterSources(mod, MobRelicGeneratedSources.All, ref mechanicOrdinal);
		if (Sets.Count != MobRelicGeneratedSources.ExpectedMobCount
			|| Items.Count != MobRelicGeneratedSources.ExpectedItemCount
			|| mechanicOrdinal != MobRelicGeneratedSources.ExpectedItemCount)
		{
			throw new InvalidOperationException(
				$"Curated relic roster mismatch: sets={Sets.Count}, items={Items.Count}, ordinals={mechanicOrdinal}.");
		}
		MobRelicLegacyAliases.Register(Sets);
	}

	private static void RegisterSources(Mod mod, IEnumerable<MobRelicSourceDefinition> sources, ref int mechanicOrdinal)
	{
		foreach (MobRelicSourceDefinition source in sources)
		{
			if (Sets.ContainsKey(source.ContentKey))
				throw new InvalidOperationException($"Duplicate generated mob identity: {source.ContentKey}");
			MobRelicTier[] tiers = Enum.GetValues<MobRelicTier>();
			GeneratedMobRelicItem[] sourceItems = new GeneratedMobRelicItem[tiers.Length];
			for (int index = 0; index < tiers.Length; index++)
				sourceItems[index] = new GeneratedMobRelicItem(source, tiers[index], mechanicOrdinal++);
			foreach (GeneratedMobRelicItem item in sourceItems)
			{
				if (!mod.AddContent(item))
					throw new InvalidOperationException($"Failed to register generated item {item.Name}.");
				Items.Add(item);
			}
			Sets.Add(source.ContentKey, new MobRelicSet(source, sourceItems));
		}
	}

	internal static bool TryGet(string contentKey, out MobRelicSet set) => Sets.TryGetValue(contentKey, out set);

	internal static void Reset()
	{
		MobRelicLegacyAliases.Reset();
		Sets.Clear();
		Items.Clear();
	}
}
