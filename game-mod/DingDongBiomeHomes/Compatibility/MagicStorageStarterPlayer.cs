using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class MagicStorageStarterPlayer : ModPlayer
{
	internal static readonly (string Name, int Stack)[] Kit =
	[
		("StorageHeart", 1), ("CraftingAccess", 1), ("StorageAccess", 1),
		// Owner, 29.10: no Tiny Storage Unit — it can be neither crafted nor uncrafted.
		("StorageUnit", 3), ("StorageConnector", 8)
	];
	internal static bool IsKitComponent(Item item)
	{
		if (item.ModItem?.Mod.Name != "MagicStorage") return false;
		foreach (var component in Kit) if (item.ModItem.Name == component.Name) return true;
		return false;
	}

	public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
	{
		// tML invokes this again when mediumcore death drops are reconstructed.
		// Only character creation receives the kit; no login/world-entry grant.
		if (mediumCoreDeath || !ModLoader.TryGetMod("MagicStorage", out Mod storage)) yield break;
		foreach ((string name, int stack) in Kit)
		{
			if (!storage.TryFind<ModItem>(name, out var item))
				throw new InvalidOperationException("Magic Storage starter component missing: " + name);
			yield return new Item(item.Type, stack);
		}
	}
}
