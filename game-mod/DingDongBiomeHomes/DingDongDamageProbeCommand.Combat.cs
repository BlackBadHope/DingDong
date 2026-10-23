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
public sealed partial class DingDongDamageProbeCommand
{
	private static bool RejectsUnknownStrikeFlags() => DamageNetworkCodec.AreSupportedStrikeFlags(7) && !DamageNetworkCodec.AreSupportedStrikeFlags(8);
	private static int FinalDamage(int baseDamage, DamageEvaluationTrace trace) => (int)Math.Round(baseDamage * trace.FinalMultiplier, MidpointRounding.AwayFromZero);
}
