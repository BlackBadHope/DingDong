using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed class DingDongGameplayProbeCommand : ModCommand
{
	public override string Command => "dingdonggameplay";
	public override CommandType Type => CommandType.Chat | CommandType.Console;
	public override string Usage => "/dingdonggameplay";
	public override string Description => "Shows the live contract state of quests, two-leg escort, half-height jump, night pressure and creature teleporters.";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		GameplayContractResult result = DingDongGameplayContracts.Evaluate();
		caller.Reply(result.Summary, result.Pass ? Color.LightGreen : Color.OrangeRed);
	}
}
