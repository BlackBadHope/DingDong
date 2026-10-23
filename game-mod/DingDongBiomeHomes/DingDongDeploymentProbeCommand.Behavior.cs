using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Read-only dedicated-server contract gate for candidate placement metadata.
/// It never places a tile and cannot enable the approved worldgen manifest.
/// </summary>
public sealed partial class DingDongDeploymentProbeCommand
{
	public override void Action(CommandCaller caller, string input, string[] args)
	{
		string mode = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();
		bool validMode = (args.Length == 1 && mode is "contract" or "sites" or "simultaneous" or "relocationplan" or "relocationapply" or "previewstatus") || (args.Length == 2 && mode == "preview") || (args.Length == 6 && mode == "relocationverify");
		if (!Main.dedServ || !string.Equals(Environment.GetEnvironmentVariable(ProbeEnvironmentVariable), "1", StringComparison.Ordinal) || !validMode)
		{
			caller.Reply($"Usage: {Usage}. Requires dedicated server and {ProbeEnvironmentVariable}=1.", Color.OrangeRed);
			return;
		}

		string worldPath = Main.ActiveWorldFileData?.Path ?? string.Empty;
		string worldFile = Path.GetFileNameWithoutExtension(worldPath);
		bool relocationMutationMode = mode is "relocationapply" or "relocationverify";
		bool previewMode = mode is "preview" or "previewstatus";
		bool liveReadOnlyPlan = mode == "simultaneous" &&
			string.Equals(Environment.GetEnvironmentVariable("DINGDONG_LIVE_READONLY_PLAN"), "1", StringComparison.Ordinal);
		bool acceptedWorld = liveReadOnlyPlan || (relocationMutationMode ? worldFile.StartsWith(RelocationProbeWorldPrefix, StringComparison.Ordinal) : previewMode ? worldFile.StartsWith(PreviewProbeWorldPrefix, StringComparison.Ordinal) : worldFile.StartsWith(ProbeWorldPrefix, StringComparison.Ordinal) || worldFile.StartsWith(RelocationProbeWorldPrefix, StringComparison.Ordinal) || worldFile.StartsWith(PreviewProbeWorldPrefix, StringComparison.Ordinal));
		if (!acceptedWorld)
		{
			string expected = relocationMutationMode ? RelocationProbeWorldPrefix : previewMode ? PreviewProbeWorldPrefix : $"{ProbeWorldPrefix}, {RelocationProbeWorldPrefix}, or {PreviewProbeWorldPrefix}";
			caller.Reply($"Probe refused: disposable world filename must start with '{expected}'.", Color.OrangeRed);
			return;
		}

		if (previewMode && !string.Equals(Environment.GetEnvironmentVariable(PreviewEnvironmentVariable), "1", StringComparison.Ordinal))
		{
			caller.Reply($"Preview refused: {PreviewEnvironmentVariable}=1 is required.", Color.OrangeRed);
			return;
		}

		if (mode == "sites")
		{
			RunSitePlan(caller, worldFile);
			return;
		}

		if (mode == "simultaneous")
		{
			RunSimultaneousPlan(caller, worldFile);
			return;
		}

		if (mode == "relocationplan")
		{
			RunRelocationPlanOrApply(caller, worldFile, apply: false);
			return;
		}

		if (mode == "relocationapply")
		{
			RunRelocationPlanOrApply(caller, worldFile, apply: true);
			return;
		}

		if (mode == "relocationverify")
		{
			RunRelocationVerify(caller, worldFile, args);
			return;
		}

		if (mode == "preview")
		{
			RunPreview(caller, worldFile, args[1]);
			return;
		}

		if (mode == "previewstatus")
		{
			RunPreviewStatus(caller, worldFile);
			return;
		}

		RunDeploymentContract(caller, worldFile);
	}
}
