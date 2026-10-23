using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class DingDongAllModEndToEndCommand : ModCommand
{
	public override string Command => "dingdongallmode2e";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdongallmode2e <prepare|verify>";
	public override string Description => "Runs the opt-in exact-mod-set E2E world phase on the dedicated server thread.";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (args.Length != 1
			|| (!string.Equals(args[0], "prepare", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(args[0], "verify", StringComparison.OrdinalIgnoreCase)))
		{
			caller.Reply($"Usage: {Usage}.", Color.OrangeRed);
			return;
		}

		string marker = ModContent.GetInstance<AllModEndToEndProbeSystem>().RunFromConsole(args[0]);
		caller.Reply(marker, marker.Contains("|verdict=PASS|", StringComparison.Ordinal) ? Color.LightGreen : Color.OrangeRed);
	}
}
