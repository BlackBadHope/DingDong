using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

internal static class HardLootBagBaseline
{
	private sealed record Expected(int Item, int Minimum, float Rate,
		IItemDropRuleCondition[] Conditions);
	private sealed record Snapshot(Expected[] Drops, string[] Errors, int Rules);
	private static readonly Dictionary<int, Snapshot> Before = [];
	internal static IEnumerable<int> Types => Before.Keys;
	internal static void Reset() => Before.Clear();
	internal static void Capture(Item item, ItemLoot loot)
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_X10_LOOT_ACCEPTANCE") != "1" ||
			!ItemID.Sets.BossBag[item.type] || ItemID.Sets.Deprecated[item.type]) return;
		// Copy report values before deferred adapters mutate their shared rule instances.
		Before[item.type] = SnapshotRules(loot.Get());
	}
	internal static Dictionary<int, int> Original(int type, DropAttemptInfo info)
	{
		if (!Before.TryGetValue(type, out Snapshot snapshot))
			throw new InvalidOperationException("pre-normalization snapshot missing");
		return Guarantees(snapshot, info);
	}
	internal static Dictionary<int, int> Current(List<IItemDropRule> rules, DropAttemptInfo info) =>
		Guarantees(SnapshotRules(rules), info);
	private static Snapshot SnapshotRules(List<IItemDropRule> rules)
	{
		List<Expected> drops = [];
		List<string> errors = [];
		foreach (IItemDropRule rule in rules)
		{
			try
			{
				List<DropRateInfo> rates = [];
				rule.ReportDroprates(rates, new DropRateInfoChainFeed(1f));
				foreach (DropRateInfo rate in rates)
					drops.Add(new Expected(rate.itemId, rate.stackMin, rate.dropRate,
						rate.conditions?.ToArray() ?? []));
			}
			catch (Exception error)
			{
				errors.Add($"{rule.GetType().FullName}:{error.GetType().Name}:{error.Message}");
			}
		}
		return new Snapshot(drops.ToArray(), errors.ToArray(), rules.Count);
	}
	private static Dictionary<int, int> Guarantees(Snapshot snapshot, DropAttemptInfo info)
	{
		if (snapshot.Errors.Length != 0) throw new InvalidOperationException(string.Join("; ", snapshot.Errors));
		if (snapshot.Rules == 0) throw new InvalidOperationException("boss bag has no item-loot registry rules");
		Dictionary<int, int> guaranteed = [];
		foreach (Expected drop in snapshot.Drops)
		{
			if (drop.Item <= 0 || drop.Item is >= ItemID.CopperCoin and <= ItemID.PlatinumCoin ||
				drop.Rate < 0.999999f || !drop.Conditions.All(condition => condition.CanDrop(info))) continue;
			guaranteed[drop.Item] = checked(guaranteed.GetValueOrDefault(drop.Item) + Math.Max(1, drop.Minimum));
		}
		return guaranteed;
	}
}
