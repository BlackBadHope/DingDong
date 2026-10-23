using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed partial class AccessoryEnchantGlobalItem
{
	public override void UpdateAccessory(Item item, Player player, bool hideVisual)
	{
		if (!item.accessory)
			return;
		AccessoryEnchantPlayer enchantPlayer = player.GetModPlayer<AccessoryEnchantPlayer>();
		foreach (AccessoryEnchantmentId enchantment in fused)
			enchantPlayer.Apply(enchantment);
	}
}
