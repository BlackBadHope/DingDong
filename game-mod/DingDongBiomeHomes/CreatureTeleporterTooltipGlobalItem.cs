using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed class CreatureTeleporterTooltipGlobalItem : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (item.type != ItemID.Teleporter)
			return;
		tooltips.Add(new TooltipLine(Mod, "DingDongCreatureTeleport", Terraria.Localization.Language.GetTextValue("Mods.DingDongBiomeHomes.Teleporter.CreatureTooltip")));
	}
}
