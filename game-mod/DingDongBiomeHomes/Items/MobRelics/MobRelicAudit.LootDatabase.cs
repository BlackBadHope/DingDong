using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using DingDongBiomeHomes.Systems.HardLoot;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditLootDatabase(MobRelicAuditContext context, NPC mob, string key, MobRelicSet set, MobRelicDropRegistration drop)
	{
		try
		{
			List<IItemDropRule> rules = Main.ItemDropsDB.GetRulesForNPCID(mob.netID, includeGlobalDrops: true);
			context.ForeignRulesEnumerated += rules.Count;
			List<DropRateInfo> reported = new();
			foreach (IItemDropRule rule in rules)
				rule.ReportDroprates(reported, new DropRateInfoChainFeed(1f));
			(int Type, float Rate, Type Condition)[] expected =
			[
				(set.CommonTrace.Type, 0.5f / drop.CommonDenominator, typeof(ClassicWorldDropCondition)),
				(set.CommonCurio.Type, 0.5f / drop.CommonDenominator, typeof(ClassicWorldDropCondition)),
				(set.ExpertSeal.Type, 1f / (drop.ExpertDenominator * 3f), typeof(ExactExpertWorldDropCondition)),
				(set.MasterRelic.Type, 1f / (drop.MasterDenominator * 3f), typeof(ExactMasterWorldDropCondition)),
				(set.LegendaryMyth.Type, 1f / (drop.LegendaryDenominator * 3f), typeof(LegendaryWorldDropCondition))
			];
			// Database rules have already passed HardLootGlobalNpc. Compare the
			// configured hardened chance, not the pre-normalization registration.
			// Missing items, zero rates and lost difficulty conditions still fail.
			int matched = expected.Count(value => reported.Any(rate => rate.itemId == value.Type && Math.Abs(rate.dropRate - value.Rate / HardLootClassifier.ExtraDivisor(value.Rate)) <= 0.000001f && rate.conditions.Any(condition => condition.GetType() == value.Condition)));
			context.ReportedDropsPassed += matched;
			if (matched == MobSpecificRelicCatalog.ItemsPerMob)
				context.LootDatabasePassed++;
			else
				Failure(context, $"loot-db:{key}:{matched}/5:" + string.Join(";", expected.Select(value =>
					$"item={value.Type},condition={value.Condition.Name},expected={value.Rate / HardLootClassifier.ExtraDivisor(value.Rate):R},reported=" +
					string.Join(",", reported.Where(rate => rate.itemId == value.Type).Select(rate => $"{rate.dropRate:R}[{string.Join('+', rate.conditions.Select(condition => condition.GetType().Name))}]")))));
		}
		catch (Exception exception)
		{
			Failure(context, $"loot-db:{key}:{exception.GetType().Name}");
		}
	}
}
