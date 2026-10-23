using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootRule : IItemDropRule
{
	private readonly IItemDropRule inner;
	private readonly int divisor;
	private readonly bool preserveBosses;

	internal HardLootRule(IItemDropRule inner, int divisor, bool preserveBosses)
	{
		this.inner = inner;
		this.divisor = System.Math.Max(1, divisor);
		this.preserveBosses = preserveBosses;
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => inner.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return new ItemDropAttemptResult { State = ItemDropAttemptResultState.DidNotRunCode };
		if (preserveBosses && info.npc?.boss == true)
			return inner.TryDroppingItem(info);
		if (divisor > 1 && info.rng.Next(divisor) != 0)
			return new ItemDropAttemptResult { State = ItemDropAttemptResultState.FailedRandomRoll };
		return inner.TryDroppingItem(info);
	}

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) =>
		inner.ReportDroprates(drops, ratesInfo.With(1f / divisor));
}
