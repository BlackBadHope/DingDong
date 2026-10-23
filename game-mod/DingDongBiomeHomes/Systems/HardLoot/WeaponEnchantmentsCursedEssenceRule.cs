using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class WeaponEnchantmentsCursedEssenceRule : IItemDropRule
{
	private readonly IItemDropRule inner;
	private readonly int divisor;
	private readonly Func<float> multiplier;

	internal WeaponEnchantmentsCursedEssenceRule(IItemDropRule inner, int divisor,
		Func<float> multiplier)
	{
		this.inner = inner;
		this.divisor = Math.Max(1, divisor);
		this.multiplier = multiplier;
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => inner.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return new() { State = ItemDropAttemptResultState.DidNotRunCode };
		NPC npc = info.npc;
		if (npc is null || !inner.CanDrop(info) || multiplier() != 1f ||
			!IsOptionalPositive(npc))
			return inner.TryDroppingItem(info);
		if (divisor > 1 && info.rng.Next(divisor) != 0)
			return new() { State = ItemDropAttemptResultState.FailedRandomRoll };
		return inner.TryDroppingItem(info);
	}

	private static bool IsOptionalPositive(NPC npc) =>
		npc.realLife == -1 && npc.netID is >= 13 and <= 15;

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) =>
		inner.ReportDroprates(drops, ratesInfo);
}
