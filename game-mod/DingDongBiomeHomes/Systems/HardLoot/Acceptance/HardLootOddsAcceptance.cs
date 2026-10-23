using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Utilities;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

internal static class HardLootOddsAcceptance
{
	internal static string Run(Player player)
	{
		IItemDropRule optional = ItemDropRule.Common(ItemID.ZombieArm, 20);
		IItemDropRule harder = HardLootRuleFactory.Wrap(optional,
			HardLootClassifier.ExtraDivisor(1f / 20), false);
		int baseline = Count(optional, player, 16000);
		int hardened = Count(harder, player, 16000);
		if (baseline < 400 || hardened <= 0 || hardened >= baseline / 2)
			throw new InvalidOperationException($"Optional rate mismatch: {baseline}->{hardened}.");
		if (HardLootClassifier.ExtraDivisor(1f / 4000) != 1)
			throw new InvalidOperationException("Ultra-rare 1/4000 chance was changed.");
		if (HardLootClassifier.Inspect(ItemDropRule.Common(ItemID.Gel), out _) !=
			HardLootDisposition.Guaranteed)
			throw new InvalidOperationException("Guaranteed rule no longer protected.");
		int guaranteed = Count(ItemDropRule.Common(ItemID.Gel), player, 32);
		if (guaranteed != 32)
			throw new InvalidOperationException($"Guaranteed emissions {guaranteed}/32.");
		return $"optionalBaseline={baseline}|optionalHardened={hardened}" +
			"|optionalTrials=16000|guaranteedEmissions=32|ultraRareDivisor=1";
	}

	private static int Count(IItemDropRule rule, Player player, int trials)
	{
		int count = 0;
		var random = new UnifiedRandom(18427);
		UnifiedRandom previous = Main.rand;
		try
		{
			// CommonDrop calls Player.RollLuck, which reads Main.rand rather than info.rng.
			Main.rand = new UnifiedRandom(18427);
			for (int index = 0; index < trials; index++)
			{
				var info = new DropAttemptInfo { item = ItemID.KingSlimeBossBag,
					player = player, IsExpertMode = true, IsMasterMode = true, rng = random };
				var items = HardLootAcceptanceCapture.Observe(() => rule.TryDroppingItem(info));
				if (items.Count > 0) count++;
			}
		}
		finally { Main.rand = previous; }
		return count;
	}
}
