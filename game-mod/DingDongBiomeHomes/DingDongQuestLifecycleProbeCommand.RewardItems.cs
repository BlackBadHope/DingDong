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
public sealed partial class DingDongQuestLifecycleProbeCommand
{
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

	private static long CoinValue(IEnumerable<Item> items)
	{
		long total = 0;
		foreach (Item item in items)
		{
			total += item.type switch
			{
				ItemID.PlatinumCoin => (long)item.stack * 1_000_000L,
				ItemID.GoldCoin => (long)item.stack * 10_000L,
				ItemID.SilverCoin => (long)item.stack * 100L,
				ItemID.CopperCoin => item.stack,
				_ => 0L
			};
		}

		return total;
	}

	private static bool Localized(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("Mods.", StringComparison.Ordinal);
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
