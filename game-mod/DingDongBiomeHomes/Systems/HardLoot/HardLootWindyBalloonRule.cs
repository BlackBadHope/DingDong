using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootWindyBalloonRule : IItemDropRule
{
	private readonly OneFromOptionsDropRule source;

	internal HardLootWindyBalloonRule(OneFromOptionsDropRule source)
	{
		this.source = source;
		ChainedRules = [.. source.ChainedRules];
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; }
	public bool CanDrop(DropAttemptInfo info) => source.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return new ItemDropAttemptResult {
				State = ItemDropAttemptResultState.DidNotRunCode };
		bool success = info.player.RollLuck(source.chanceDenominator) <
			source.chanceNumerator;
		return new ItemDropAttemptResult { State = success
			? ItemDropAttemptResultState.Success
			: ItemDropAttemptResultState.FailedRandomRoll };
	}

	public void ReportDroprates(List<DropRateInfo> drops,
		DropRateInfoChainFeed ratesInfo) => source.ReportDroprates(drops, ratesInfo);
}
