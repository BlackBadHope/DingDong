using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
/// <summary>
/// The plain-text view model shared by the interactive guide and dedicated
/// audits. All explanations are derived from the same profiles used by combat.
/// </summary>
internal static partial class DamageGuideContent
{
	private static bool NearlyOne(double value) => Math.Abs(value - 1d) <= 0.000001d;
	private static DamageGuideTone Tone(double multiplier) => multiplier >= 1.075d ? DamageGuideTone.Positive : multiplier <= 0.925d ? DamageGuideTone.Information : DamageGuideTone.Normal;
	private static string Text(string key, params object[] args) => Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.Guide.{key}", args);
}
