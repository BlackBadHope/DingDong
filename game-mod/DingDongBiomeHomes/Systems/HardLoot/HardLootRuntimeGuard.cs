using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootRuntimeGuard
{
	internal static void EnforceBeforeNpcLoot()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		LootBonusNeutralizer.EnforceBeforeLoot();
		RandomLootGate.EnforceBeforeLoot();
	}

	internal static void AssertCurrent()
	{
		if (!LootBonusNeutralizer.IsNeutralized)
			throw new InvalidOperationException("Hard-loot final gate found LootBonus drift.");
		if (!RandomLootGate.IsDisabled)
			throw new InvalidOperationException("Hard-loot final gate found RandomLoot NPC bypass enabled.");
	}
}
