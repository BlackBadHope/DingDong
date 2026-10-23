using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

public sealed class X10MultiplayerProbeCommand : ModCommand
{
	public override string Command => "ddx10mp";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "ddx10mp <route|sample> <forest|sahara|arctic|amazon>";
	public override string Description => "Opt-in disposable x10 multiplayer acceptance.";
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		string[] request = (string[])args.Clone();
		Main.QueueMainThreadAction(() =>
		{
			try
			{
				X10MultiplayerProbe.RequireServer();
				if (request.Length != 2) throw new ArgumentException(Usage);
				if (request[0] == "route") X10MultiplayerProbe.Route(request[1]);
				else if (request[0] == "sample") X10MultiplayerProbe.Sample(request[1]);
				else throw new ArgumentException(Usage);
			}
			catch (Exception error)
			{
				X10MultiplayerProbe.Emit($"DINGDONG_X10_MP_SERVER|verdict=FAIL|reason={error.Message.Replace('|', '/')}");
			}
		});
	}
}
