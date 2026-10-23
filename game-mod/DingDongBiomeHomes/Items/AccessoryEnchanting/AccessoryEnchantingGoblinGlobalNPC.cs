using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed class AccessoryEnchantingGoblinGlobalNPC : GlobalNPC
{
	public override void GetChat(NPC npc, ref string chat)
	{
		if (npc.type != NPCID.GoblinTinkerer)
			return;
		chat += " " + Language.GetTextValue("Mods.DingDongBiomeHomes.NpcDialogue.GoblinFusionHint");
	}
}
