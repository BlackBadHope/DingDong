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
public sealed partial class DingDongDeploymentProbeCommand : ModCommand
{
	private const string ProbeEnvironmentVariable = "DINGDONG_HOUSING_PROBE";
	private const string PreviewEnvironmentVariable = "DINGDONG_PREVIEW_PROBE";
	private const string ProbeWorldPrefix = "DingDongHousingProbe-";
	private const string RelocationProbeWorldPrefix = "DingDongRelocationProbe-";
	private const string PreviewProbeWorldPrefix = "DingDongPreviewProbe-";
	private const string ScarletNpcKey = "Terraria/PartyGirl";
	public override string Command => "dingdongdeploymentprobe";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdongdeploymentprobe <contract|sites|simultaneous|relocationplan|relocationapply|relocationverify sourceX sourceY destinationX destinationY signature|preview npcKey|previewstatus>";
	public override string Description => "Validates candidate deployment or creates one gated disposable visual preview.";
}
