using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes;

internal readonly record struct ThematicRewardOption(int ItemType, int MinStack, int MaxStack);
internal readonly record struct ThematicRewardRoll(int ItemType, int Stack);

internal static partial class QuestThematicReward
{
	internal const int MinimumNpcVariety = 70;
	internal const int MinimumCacheVariety = 120;

	internal static ThematicRewardRoll Roll(NpcTownIdentity identity, int difficultyRank)
	{
		ThematicRewardOption[] archetype = ArchetypePool(identity.Archetype);
		ThematicRewardOption[] biome = BiomePool(identity.Biome);
		ThematicRewardOption[] personal = PersonalPool(identity, archetype.Concat(biome));
		int branch = Main.rand.Next(20);
		ThematicRewardOption[] pool = branch < 9 ? archetype : branch < 15 ? biome : personal;
		ThematicRewardOption option = Scale(pool[Main.rand.Next(pool.Length)], difficultyRank);
		return new ThematicRewardRoll(option.ItemType, Main.rand.Next(option.MinStack, option.MaxStack + 1));
	}

	internal static ThematicRewardOption[] NpcPool(NpcTownIdentity identity)
	{
		ThematicRewardOption[] archetype = ArchetypePool(identity.Archetype);
		ThematicRewardOption[] biome = BiomePool(identity.Biome);
		return Distinct(archetype.Concat(biome).Concat(PersonalPool(identity, archetype.Concat(biome)))).ToArray();
	}

	internal static ThematicRewardOption[] CachePool(int difficultyRank)
	{
		List<ThematicRewardOption> all = Distinct(SupplementalPool
			.Concat(Enum.GetValues<HomeArchetype>().SelectMany(ArchetypePool))
			.Concat(Enum.GetValues<HomeBiome>().SelectMany(BiomePool))).ToList();
		int start = Math.Clamp(difficultyRank, 0, 3) * 29 % all.Count;
		return Enumerable.Range(0, Math.Min(MinimumCacheVariety, all.Count))
			.Select(index => Scale(all[(start + index) % all.Count], difficultyRank))
			.ToArray();
	}

	internal static bool ContractPass() =>
		SupplementalPool.Select(option => option.ItemType).Distinct().Count() >= MinimumCacheVariety &&
		CachePool(0).Length >= MinimumCacheVariety && CachePool(3).Length >= MinimumCacheVariety &&
		NpcTownManifest.All.All(identity => NpcPool(identity).Length >= MinimumNpcVariety);

	private static ThematicRewardOption[] PersonalPool(NpcTownIdentity identity, IEnumerable<ThematicRewardOption> excludedOptions)
	{
		HashSet<int> excluded = excludedOptions.Select(option => option.ItemType).ToHashSet();
		List<ThematicRewardOption> source = Distinct(SupplementalPool).Where(option => !excluded.Contains(option.ItemType)).ToList();
		int required = Math.Max(1, MinimumNpcVariety - excluded.Count);
		int start = (int)(StableHash(identity.NpcKey) % (uint)source.Count);
		return Enumerable.Range(0, Math.Min(required, source.Count)).Select(index => source[(start + index) % source.Count]).ToArray();
	}

	private static IEnumerable<ThematicRewardOption> Distinct(IEnumerable<ThematicRewardOption> source) =>
		source.GroupBy(option => option.ItemType).Select(group => new ThematicRewardOption(group.Key, group.Min(option => option.MinStack), group.Max(option => option.MaxStack)));

	private static ThematicRewardOption Scale(ThematicRewardOption option, int difficultyRank)
	{
		int rank = Math.Clamp(difficultyRank, 0, 3);
		if (option.MaxStack <= 1)
			return option;
		return option with { MinStack = option.MinStack + rank, MaxStack = option.MaxStack + rank * 2 };
	}

	private static uint StableHash(string value)
	{
		uint hash = 2166136261;
		foreach (char character in value)
			hash = (hash ^ character) * 16777619;
		return hash;
	}

	private static ThematicRewardOption O(int itemType, int minStack = 1, int maxStack = 1) => new(itemType, minStack, maxStack);
}
