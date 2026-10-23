using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootNestedRule : IItemDropRule, INestedItemDropRule
{
	private readonly IItemDropRule inner;
	private readonly int divisor;
	private readonly bool preserveBosses;

	internal HardLootNestedRule(IItemDropRule inner, int divisor, bool preserveBosses)
	{
		this.inner = inner;
		this.divisor = System.Math.Max(1, divisor);
		this.preserveBosses = preserveBosses;
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => inner.CanDrop(info);
	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) =>
		new() { State = ItemDropAttemptResultState.DidNotRunCode };

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info,
		ItemDropRuleResolveAction resolveAction)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return new ItemDropAttemptResult { State = ItemDropAttemptResultState.DidNotRunCode };
		if (preserveBosses && info.npc?.boss == true)
			return resolveAction(inner, info);
		if (divisor > 1 && info.rng.Next(divisor) != 0)
			return new ItemDropAttemptResult { State = ItemDropAttemptResultState.FailedRandomRoll };
		return resolveAction(inner, info);
	}

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) =>
		HardLootReportSemantics.ReportGameplayRates(
			inner, drops, ratesInfo.With(1f / divisor));
}
