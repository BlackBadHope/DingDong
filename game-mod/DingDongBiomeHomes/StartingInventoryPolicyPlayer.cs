using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using DingDongBiomeHomes.Compatibility;

namespace DingDongBiomeHomes;

/// <summary>
/// Keeps new characters on Terraria's normal starting inventory while the
/// exact mod pack is enabled. tModLoader groups starting items by provider,
/// so foreign bonuses can be removed without guessing item IDs or touching
/// an existing character's inventory.
/// </summary>
public sealed class StartingInventoryPolicyPlayer : ModPlayer
{
	public override void ModifyStartingInventory(
		IReadOnlyDictionary<string, List<Item>> itemsByMod,
		bool mediumCoreDeath)
	{
		if (mediumCoreDeath)
			return;

		foreach ((string provider, List<Item> items) in itemsByMod)
		{
			if (provider.Equals(Mod.Name, StringComparison.Ordinal))
				items.RemoveAll(item => !MagicStorageStarterPlayer.IsKitComponent(item));
			else if (!provider.Equals("Terraria", StringComparison.Ordinal))
				items.Clear();
		}
	}
}
