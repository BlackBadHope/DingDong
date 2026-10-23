using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Non-mutating dedicated-server contract probe for the deterministic damage
/// core, persistent affinity metadata and multiplayer authority protocol.
/// </summary>
public sealed partial class DingDongDamageProbeCommand : ModCommand
{
	private const string ProbeEnvironmentVariable = "DINGDONG_DAMAGE_PROBE";
	private const string ProbeWorldPrefix = "DingDongDamageProbe-";
	public override string Command => "dingdongdamageprobe";
	public override CommandType Type => CommandType.Console;
	public override string Usage => "dingdongdamageprobe slime";
	public override string Description => "Runs the non-mutating Damage System authority and coverage probe.";
}
