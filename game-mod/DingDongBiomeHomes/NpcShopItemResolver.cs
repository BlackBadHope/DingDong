using System;
using Terraria.ID;

namespace DingDongBiomeHomes;

internal static class NpcShopItemResolver
{
	private static readonly string[] SafeFallbackItemNames =
	{
		"Book",
		"Bottle",
		"Candelabra",
		"Piano",
		"Dresser",
		"Bench",
		"Bathtub",
		"GrandfatherClock",
		"HeartLantern",
		"StarinaBottle",
		"Campfire",
		"Sign"
	};

	public static int Resolve(string preferredItemName, int variant, Func<int, bool> conflicts = null)
	{
		if (ItemID.Search.TryGetId(preferredItemName, out int preferredType) &&
			preferredType > ItemID.None &&
			(conflicts is null || !conflicts(preferredType)))
		{
			return preferredType;
		}

		for (int offset = 0; offset < SafeFallbackItemNames.Length; offset++)
		{
			string itemName = SafeFallbackItemNames[(variant + offset) % SafeFallbackItemNames.Length];
			if (!ItemID.Search.TryGetId(itemName, out int itemType) || itemType <= ItemID.None)
				continue;

			if (conflicts is null || !conflicts(itemType))
				return itemType;
		}

		return ItemID.None;
	}
}
