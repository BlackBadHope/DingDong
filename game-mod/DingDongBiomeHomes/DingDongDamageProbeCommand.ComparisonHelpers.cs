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
	private static bool HasOnlyTag(WeightedDamageAxis axis, string expected) => axis.Tags.Count == 1 && axis.Tags[0].Id == expected && Nearly(axis.Tags[0].Weight, 1d);
	private static bool Nearly(double actual, double expected) => Math.Abs(actual - expected) <= 0.000001d;
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
