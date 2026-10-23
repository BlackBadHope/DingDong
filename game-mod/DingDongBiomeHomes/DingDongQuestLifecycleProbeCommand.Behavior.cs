using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		string[] request = (string[])args.Clone();
		if (Main.dedServ && string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) && !Main.player.Any(player => player.active))
		{
			RunOnMainThread(caller, request);
			return;
		}

		_ = Main.RunOnMainThread(() => RunOnMainThread(caller, request));
	}

	private void RunOnMainThread(CommandCaller caller, string[] args)
	{
		string worldFile = Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData?.Path ?? string.Empty);
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) || args.Length != 1 || !string.Equals(args[0], "lifecycle", StringComparison.OrdinalIgnoreCase) || !worldFile.StartsWith(ProbeWorldPrefix, StringComparison.Ordinal) || Main.player.Any(player => player.active))
		{
			caller.Reply($"Usage: {Usage}. Requires an empty dedicated server, {ProbeEnvironmentVariable}=1, and a world named {ProbeWorldPrefix}*.", Color.OrangeRed);
			return;
		}

		string summary;
		try
		{
			summary = RunProbe(Mod, worldFile);
		}
		catch (Exception exception)
		{
			Main.player[SyntheticPlayerIndex].active = false;
			Main.npc[SyntheticNpcIndex].active = false;
			summary = $"DINGDONG_QUEST_LIFECYCLE_PROBE|verdict=FAIL|exception={exception.GetType().Name}|message={OneLine(exception.Message)}|world={OneLine(worldFile)}|userSavesTouched=False";
			Mod.Logger.Error(summary);
		}

		caller.Reply(summary, summary.Contains("|verdict=PASS|", StringComparison.Ordinal) ? Color.LightGreen : Color.OrangeRed);
	}
}
