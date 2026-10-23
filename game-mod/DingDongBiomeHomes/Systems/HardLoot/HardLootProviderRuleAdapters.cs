using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootProviderRuleAdapters
{
	internal static void Initialize()
	{
		CalamityPityDropAdapter.Initialize();
		CalamityPityItemLootAdapter.Initialize();
		CalamityPerPlayerAdapter.Initialize();
		ThoriumMultipleRuleAdapter.Initialize();
		ThoriumMultipleItemLootAdapter.Initialize();
		WeaponEnchantmentsDifficultyAdapter.Initialize();
		WeaponEnchantmentsCursedEssenceAdapter.Initialize();
		AndroLibBasicDifficultyAdapter.Initialize();
		AndroLibWeightedDifficultyAdapter.Initialize();
		AndroLibBagDifficultyAdapter.Initialize();
		AssortedCrazyThingsLocalDropAdapter.Initialize();
		AssortedCrazyThingsOptionsDropAdapter.Initialize();
		MagicStoragePityDropAdapter.Initialize();
	}

	internal static bool TryNormalize(IItemDropRule rule, out IItemDropRule normalized)
	{
		normalized = rule;
		return HardLootWindyBalloonAdapter.TryNormalize(rule, out normalized)
			|| AssortedCrazyThingsLocalDropAdapter.TryNormalize(rule)
			|| AssortedCrazyThingsOptionsDropAdapter.TryNormalize(rule)
			|| MagicStoragePityDropAdapter.TryNormalize(rule)
			|| CalamityPityDropAdapter.TryNormalize(rule)
			|| CalamityPerPlayerAdapter.TryNormalize(rule)
			|| ThoriumMultipleRuleAdapter.TryNormalize(rule)
			|| WeaponEnchantmentsCursedEssenceAdapter.TryNormalize(rule, out normalized)
			|| WeaponEnchantmentsDifficultyAdapter.TryNormalize(rule)
			|| AndroLibBasicDifficultyAdapter.TryNormalize(rule)
			|| AndroLibWeightedDifficultyAdapter.TryNormalize(rule);
	}

	internal static void NormalizeDeferred()
	{
		CalamityPityItemLootAdapter.NormalizeDeferred();
		ThoriumMultipleItemLootAdapter.NormalizeDeferred();
		AndroLibBagDifficultyAdapter.NormalizeDeferred();
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		Attempt(MagicStoragePityDropAdapter.RestoreAndReset, failures);
		Attempt(AssortedCrazyThingsOptionsDropAdapter.RestoreAndReset, failures);
		Attempt(AssortedCrazyThingsLocalDropAdapter.RestoreAndReset, failures);
		Attempt(AndroLibBagDifficultyAdapter.RestoreAndReset, failures);
		Attempt(AndroLibWeightedDifficultyAdapter.RestoreAndReset, failures);
		Attempt(AndroLibBasicDifficultyAdapter.RestoreAndReset, failures);
		Attempt(WeaponEnchantmentsCursedEssenceAdapter.Reset, failures);
		Attempt(WeaponEnchantmentsDifficultyAdapter.RestoreAndReset, failures);
		Attempt(CalamityPityItemLootAdapter.Reset, failures);
		Attempt(ThoriumMultipleItemLootAdapter.Reset, failures);
		Attempt(ThoriumMultipleRuleAdapter.RestoreAndReset, failures);
		Attempt(CalamityPerPlayerAdapter.RestoreAndReset, failures);
		Attempt(CalamityPityDropAdapter.RestoreAndReset, failures);
		if (failures.Count != 0)
			throw new AggregateException("Provider drop adapters could not be restored.", failures);
	}

	private static void Attempt(Action action, List<Exception> failures)
	{
		try { action(); }
		catch (Exception error) { failures.Add(error); }
	}
}
