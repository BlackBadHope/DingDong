using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed class SynergyArmorGlobalItem : GlobalItem
{
	public override string IsArmorSet(Item head, Item body, Item legs) => SynergyArmorCatalog.TryMatch(head, body, legs, out SynergyArmorSet set) ? set.Definition.Key : string.Empty;
	public override void UpdateArmorSet(Player player, string set)
	{
		if (!SynergyArmorCatalog.TryGetDefinition(set, out SynergyArmorSetDefinition definition))
			return;
		player.setBonus = string.Join('\n', definition.LocalizedSetBonus(), SynergyArmorText.Format("Labels.Synergy", "Relic synergy: {0}", definition.LocalizedSynergy()));
		player.GetModPlayer<SynergyArmorPlayer>().Activate(definition);
	}
}
