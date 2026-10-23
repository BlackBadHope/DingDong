using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static partial class SynergyArmorAudit
{
	private static Item NewItem(int type)
	{
		Item item = new();
		item.SetDefaults(type);
		return item;
	}

	private static bool Localized(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("Mods.", StringComparison.Ordinal);
	private static void Failure(List<string> failures, string value)
	{
		if (failures.Count < 32)
			failures.Add(value);
	}
}
