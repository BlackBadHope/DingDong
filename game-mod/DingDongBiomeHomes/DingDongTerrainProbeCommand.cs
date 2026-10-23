using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class DingDongTerrainProbeCommand : ModCommand
{
	public override string Command => "dingdongterrainprobe";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdongterrainprobe npcKey";
	public override string Description => "Read-only candidate terrain diagnostics on a disposable saved-world copy.";
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData?.Path ?? "").StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal) || args.Length != 1)
		{
			caller.Reply("Requires dedicated probe environment and DingDongTerrainProbe- world copy. " + Usage);
			return;
		}
		var profile = AuthoredCandidateDeploymentManifest.All.FirstOrDefault(p => p.NpcKey == args[0]);
		if (profile == null) { caller.Reply("Unknown NPC key."); return; }
		try { CandidateDeploymentSitePlanner.DiagnoseTerrain(Mod, profile); }
		catch (Exception ex) { Mod.Logger.Error("DINGDONG_TERRAIN_PROBE_ERROR", ex); }
		finally
		{
			string end = "DINGDONG_TERRAIN_PROBE_END|npc=" + profile.NpcKey;
			Mod.Logger.Info(end);
			caller.Reply(end);
		}
	}
}
