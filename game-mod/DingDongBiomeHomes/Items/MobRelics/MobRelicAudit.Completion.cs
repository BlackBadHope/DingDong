using System;
using System.Linq;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static MobRelicAuditResult CompleteAudit(MobRelicAuditContext context)
	{
		int expectedMobs = MobRelicSourceCatalog.ExpectedMobCount;
		int expectedItems = MobRelicSourceCatalog.ExpectedItemCount;
		int providers = MobRelicSourceCatalog.All.Select(source => source.Provider).Distinct(StringComparer.Ordinal).Count();
		const int normalizationPassed = 0;
		const int difficultyBoostRulesRemoved = 0;
		const int rareBaseItemsPreserved = 0;
		int serviceActorsExcluded = NpcLootEligibility.ExplicitlyExcludedContent.Count(key => !context.Generated.ContainsKey(key) && !context.Sets.ContainsKey(key) && !context.Drops.ContainsKey(key) && !QuestContentRegistry.TryGetNpcTargetType(key, out _));
		bool passed = CoreCountsMatch(context, expectedMobs, expectedItems)
			&& context.UniqueMechanics.Count == expectedItems
			&& Enum.GetValues<MobRelicTrigger>().Length == MobRelicGeneratedSources.ExpectedTriggerCount
			&& Enum.GetValues<MobRelicInvocation>().Length == MobRelicGeneratedSources.ExpectedInvocationCount
			&& Enum.GetValues<MobRelicTwist>().Length == MobRelicGeneratedSources.ExpectedTwistCount
			&& context.UniqueTriggers.Count == MobRelicGeneratedSources.ExpectedTriggerCount
			&& context.UniqueInvocations.Count == MobRelicGeneratedSources.ExpectedInvocationCount
			&& context.UniqueTwists.Count == MobRelicGeneratedSources.ExpectedTwistCount
			&& MobRelicLegacyAliases.RegisteredCount == MobRelicLegacyAliases.ExpectedAliasCount
			&& context.LootDatabasePassed == expectedMobs && context.ReportedDropsPassed == expectedItems
			&& context.ClonePassed == expectedItems && context.StackPassed == expectedItems && context.WorldStackPassed == expectedItems
			&& context.ContentSamplesPassed == expectedItems && context.ForeignRulesEnumerated >= expectedMobs * 4
			&& difficultyBoostRulesRemoved == 0
			&& serviceActorsExcluded == NpcLootEligibility.ExplicitlyExcludedContent.Count
			&& context.LoadedKeys.SetEquals(context.GeneratedKeys) && context.Failures.Count == 0;

		MobRelicAuditResult result = new(passed, context.PoolsPassed, expectedMobs, context.ItemsPassed, expectedItems, context.SaveLoadPassed, context.NetPassed, context.QuestIdentityPassed, context.FunctionalItemsPassed, context.PlayerVisualsPassed, context.ResonanceProjectilePassed, context.RuntimeGameplayPassed, context.LootDatabasePassed, context.ReportedDropsPassed, context.ClonePassed, context.StackPassed, context.WorldStackPassed, context.ContentSamplesPassed, context.ForeignRulesEnumerated, context.TexturesPassed, context.LocalizationPassed, context.UniqueMechanics.Count, context.UniqueTriggers.Count, context.UniqueInvocations.Count, context.UniqueTwists.Count, context.UniqueTypes.Count, context.UniqueNames.Count, context.UniqueTexturePaths.Count, context.UniqueVisualSignatures.Count, providers, context.EligibleMobs.Count, normalizationPassed, difficultyBoostRulesRemoved, rareBaseItemsPreserved, serviceActorsExcluded, context.Failures);
		if (passed) context.Mod.Logger.Info(result.ToMarker()); else context.Mod.Logger.Error(result.ToMarker());
		return result;
	}

	private static bool CoreCountsMatch(MobRelicAuditContext context, int expectedMobs, int expectedItems)
	{
		return context.LoadedMobs.Count == expectedMobs && context.Generated.Count == expectedMobs && context.Sets.Count == expectedMobs && context.Drops.Count == expectedMobs
			&& context.RegisteredItems.Count == expectedItems && context.PoolsPassed == expectedMobs && context.ItemsPassed == expectedItems
			&& context.UniqueTypes.Count == expectedItems && context.UniqueNames.Count == expectedItems && context.UniqueTexturePaths.Count == MobRelicSourceCatalog.ExpectedTextureCount
			&& context.UniqueVisualSignatures.Count == expectedItems
			&& context.TexturesPassed == expectedItems && context.LocalizationPassed == expectedItems * 2 && context.SaveLoadPassed == expectedItems && context.NetPassed == expectedItems
			&& context.QuestIdentityPassed == expectedMobs && context.FunctionalItemsPassed == expectedItems && context.PlayerVisualsPassed == expectedItems
			&& context.ResonanceProjectilePassed == 1 && context.RuntimeGameplayPassed == 1;
	}
}
