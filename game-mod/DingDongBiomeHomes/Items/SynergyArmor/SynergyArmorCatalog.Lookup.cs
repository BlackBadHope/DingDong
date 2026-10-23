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
internal static partial class SynergyArmorCatalog
{
	internal static bool TryMatch(Item head, Item body, Item legs, out SynergyArmorSet set)
	{
		set = null;
		if (head?.ModItem is not GeneratedSynergyArmorItem headItem || body?.ModItem is not GeneratedSynergyArmorItem bodyItem || legs?.ModItem is not GeneratedSynergyArmorItem legItem || headItem.Piece != SynergyArmorPiece.Head || bodyItem.Piece != SynergyArmorPiece.Body || legItem.Piece != SynergyArmorPiece.Legs || headItem.Definition.Id != bodyItem.Definition.Id || headItem.Definition.Id != legItem.Definition.Id)
			return false;
		return Sets.TryGetValue(headItem.Definition.Id, out set);
	}
}
