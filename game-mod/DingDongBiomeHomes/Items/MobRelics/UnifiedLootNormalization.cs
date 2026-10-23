using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal readonly record struct LootNormalizationRegistration(
	string ContentKey,
	int RulesScanned,
	int DifficultyDuplicateRulesRemoved,
	int DifficultyDuplicateItemsRemoved,
	int RareBaseItemsPreserved,
	int ReportFailures);

internal static class UnifiedLootNormalization
{
	private static readonly string[] DifficultyTokens =
	[
		"Expert", "Master", "Legendary", "Revenge", "Death", "Malice", "Eternity",
		"Masochist", "Menace", "ForTheWorthy", "GetGoodWorld", "Infernum"
	];
	private static readonly Dictionary<string, LootNormalizationRegistration> Registrations = new(StringComparer.Ordinal);

	internal static IReadOnlyDictionary<string, LootNormalizationRegistration> Snapshot => Registrations;

	internal static void Normalize(NPC npc, NPCLoot npcLoot)
	{
		string key = QuestContentRegistry.ContentKey(npc);
		List<IItemDropRule> rules = npcLoot.Get(includeGlobalDrops: false).ToList();
		Dictionary<IItemDropRule, List<DropRateInfo>> reports = new();
		int reportFailures = 0;
		foreach (IItemDropRule rule in rules)
		{
			try
			{
				List<DropRateInfo> values = new();
				rule.ReportDroprates(values, new DropRateInfoChainFeed(1f));
				reports[rule] = values;
			}
			catch
			{
				reportFailures++;
				reports[rule] = new List<DropRateInfo>();
			}
		}

		HashSet<int> baseItems = reports.Values.SelectMany(values => values)
			.Where(value => value.itemId > 0 && !HasDifficultyCondition(value))
			.Select(value => value.itemId)
			.ToHashSet();
		HashSet<int> rareBaseItems = reports.Values.SelectMany(values => values)
			.Where(value => value.itemId > 0
				&& value.dropRate > 0f
				&& value.dropRate <= 1f / 4000f + 0.0000001f
				&& !HasDifficultyCondition(value))
			.Select(value => value.itemId)
			.ToHashSet();

		int removedRules = 0;
		HashSet<int> removedItems = new();
		foreach ((IItemDropRule rule, List<DropRateInfo> values) in reports)
		{
			if (values.Count == 0
				|| !values.All(value => value.itemId > 0 && HasDifficultyCondition(value))
				|| !values.All(value => baseItems.Contains(value.itemId)))
				continue;
			npcLoot.Remove(rule);
			removedRules++;
			foreach (DropRateInfo value in values) removedItems.Add(value.itemId);
		}

		LootNormalizationRegistration registration = new(
			key,
			rules.Count,
			removedRules,
			removedItems.Count,
			rareBaseItems.Count,
			reportFailures);
		if (Registrations.TryGetValue(key, out LootNormalizationRegistration existing))
		{
			Registrations[key] = new LootNormalizationRegistration(
				key,
				existing.RulesScanned + registration.RulesScanned,
				existing.DifficultyDuplicateRulesRemoved + registration.DifficultyDuplicateRulesRemoved,
				existing.DifficultyDuplicateItemsRemoved + registration.DifficultyDuplicateItemsRemoved,
				existing.RareBaseItemsPreserved + registration.RareBaseItemsPreserved,
				existing.ReportFailures + registration.ReportFailures);
		}
		else
		{
			Registrations.Add(key, registration);
		}
	}

	private static bool IsDifficultyCondition(IItemDropRuleCondition condition)
	{
		string name = condition?.GetType().FullName ?? string.Empty;
		return DifficultyTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase));
	}

	private static bool HasDifficultyCondition(DropRateInfo value) =>
		value.conditions?.Any(IsDifficultyCondition) == true;

	internal static void Reset() => Registrations.Clear();
}
