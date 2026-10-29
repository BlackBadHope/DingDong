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
public sealed partial class DingDongQuestLifecycleProbeCommand : ModCommand
{
	private const string ProbeEnvironmentVariable = "DINGDONG_QUEST_PROBE";
	private const string ProbeWorldPrefix = "DingDongQuestProbe-";
	internal const string ProbeIdentity = "00112233445566778899aabbccddeeff";
	public override string Command => "dingdongquestprobe";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdongquestprobe lifecycle|forms";
	public override string Description => "Runs an isolated dedicated-server quest acceptance, completion, reward and persistence probe.";
}
