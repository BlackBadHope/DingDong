using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootDifficultyBranchAdapter
{
	internal static bool TryNormalize(ref IItemDropRule rule)
	{
		if (HardLootProviderRuleAdapters.TryNormalize(
			rule, out IItemDropRule providerRule))
		{
			rule = providerRule;
			return true;
		}
		if (HardLootFailureFallbackNormalizer.TryNormalize(
			rule, 0, out IItemDropRule fallbackRule))
		{
			rule = fallbackRule;
			return true;
		}
		return false;
	}
}
