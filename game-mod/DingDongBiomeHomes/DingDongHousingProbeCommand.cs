using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Dedicated-server-only destructive probe for disposable worlds. Both the
/// environment flag and world-name prefix prevent use in ordinary worlds.
/// </summary>
public sealed partial class DingDongHousingProbeCommand : ModCommand
{
	private const string ProbeEnvironmentVariable = "DINGDONG_HOUSING_PROBE";
	private const string ProbeWorldPrefix = "DingDongHousingProbe-";

	public override string Command => "dingdonghousingprobe";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdonghousingprobe <all|reserve|failed-shells|replacements|showcase|intake|roadmap-r6|roadmap-r7|gallery>";
	public override string Description => "Runs the authored housing baseline in a disposable dedicated-server world.";

	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (!TryCreateRequest(caller, args, out HousingProbeRequest request))
			return;
		if (request.Mode == HousingProbeMode.Gallery)
		{
			RunReplacementGallery(caller, request.WorldFile);
			return;
		}

		RunBaseline(caller, request);
	}

	private bool TryCreateRequest(CommandCaller caller, string[] args, out HousingProbeRequest request)
	{
		request = default;
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) || args.Length != 1 || !HousingProbeRequest.TryParse(args[0], out HousingProbeMode mode))
		{
			caller.Reply($"Usage: {Usage}. Requires dedicated server and {ProbeEnvironmentVariable}=1.", Color.OrangeRed);
			return false;
		}

		string worldFile = Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? string.Empty);
		if (!worldFile.StartsWith(ProbeWorldPrefix, StringComparison.Ordinal))
		{
			caller.Reply($"Probe refused: disposable world filename must start with '{ProbeWorldPrefix}'.", Color.OrangeRed);
			return false;
		}

		request = new HousingProbeRequest(mode, worldFile);
		return true;
	}
}
