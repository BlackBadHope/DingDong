using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootModeRule : IItemDropRule, INestedItemDropRule
{
	private readonly IItemDropRule defaultRule;
	private readonly IItemDropRule harderRule;
	private readonly Func<bool> harderMode;
	private readonly IItemDropRuleCondition defaultCondition;
	private readonly IItemDropRuleCondition harderCondition;

	internal HardLootModeRule(IItemDropRule defaultRule, IItemDropRule harderRule,
		Func<bool> harderMode, IItemDropRuleCondition defaultCondition,
		IItemDropRuleCondition harderCondition, IEnumerable<IItemDropRuleChainAttempt> chains)
	{
		this.defaultRule = defaultRule;
		this.harderRule = harderRule;
		this.harderMode = harderMode;
		this.defaultCondition = defaultCondition;
		this.harderCondition = harderCondition;
		ChainedRules.AddRange(chains);
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	private IItemDropRule Selected => harderMode() ? harderRule : defaultRule;
	public bool CanDrop(DropAttemptInfo info) => Selected.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) =>
		new() { State = ItemDropAttemptResultState.DidNotRunCode };

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info,
		ItemDropRuleResolveAction resolveAction) => resolveAction(Selected, info);

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
	{
		DropRateInfoChainFeed harderFeed = ratesInfo.With(1f);
		harderFeed.AddCondition(harderCondition);
		harderRule.ReportDroprates(drops, harderFeed);
		DropRateInfoChainFeed defaultFeed = ratesInfo.With(1f);
		defaultFeed.AddCondition(defaultCondition);
		defaultRule.ReportDroprates(drops, defaultFeed);
		Chains.ReportDroprates(ChainedRules, 1f, drops, ratesInfo);
	}
}
