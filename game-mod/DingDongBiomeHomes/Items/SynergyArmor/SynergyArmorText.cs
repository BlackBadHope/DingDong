using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static class SynergyArmorText
{
	internal static string Format(string keySuffix, string englishFallback, params object[] args)
	{
		string key = $"Mods.DingDongBiomeHomes.SynergyArmor.{keySuffix}";
		string value = Language.GetTextValue(key, args);
		return value == key ? string.Format(CultureInfo.InvariantCulture, englishFallback, args) : value;
	}
}
