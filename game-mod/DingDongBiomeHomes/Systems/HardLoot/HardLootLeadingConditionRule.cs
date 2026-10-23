using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootLeadingConditionRule : IItemDropRule
{
	private readonly IItemDropRuleCondition condition;

	internal HardLootLeadingConditionRule(IItemDropRuleCondition condition,
		IEnumerable<IItemDropRuleChainAttempt> chains)
	{
		this.condition = condition;
		ChainedRules.AddRange(chains);
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => condition.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) =>
		new() { State = ItemDropAttemptResultState.Success };

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
	{
		ratesInfo.AddCondition(condition);
		Chains.ReportDroprates(ChainedRules, 1f, drops, ratesInfo);
	}
}
