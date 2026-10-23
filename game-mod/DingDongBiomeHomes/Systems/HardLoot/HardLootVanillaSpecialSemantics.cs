using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootVanillaSpecialSemantics
{
	internal static HardLootChanceKind Inspect(IItemDropRule rule)
	{
		if (rule.GetType() == typeof(MechBossSpawnersDropRule))
			return HardLootChanceKind.Optional;
		if (rule.GetType() == typeof(SlimeBodyItemDropRule))
			return HardLootChanceKind.ConditionalGuaranteed;
		return HardLootChanceKind.Unknown;
	}
}
