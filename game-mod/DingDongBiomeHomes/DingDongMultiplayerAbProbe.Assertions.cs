using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class DingDongMultiplayerAbProbe
{
	private static bool[] CaptureActiveItems() => Main.item.Select(item => item?.active == true).ToArray();
	private static List<Item> NewWorldItems(bool[] before)
	{
		List<Item> items = new();
		for (int index = 0; index < Main.item.Length; index++)
		{
			if ((index >= before.Length || !before[index]) && Main.item[index]?.active == true)
				items.Add(Main.item[index]);
		}

		return items;
	}

	private static void Check(List<string> failures, string name, bool condition, ref int passed)
	{
		if (condition)
			passed++;
		else
			Fail(failures, name);
	}

	private static void Fail(List<string> failures, string value)
	{
		if (failures.Count < 32)
			failures.Add(value.Replace('|', '/'));
	}
}
