using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	private static void DrawNpcChatButtons(OrigDrawNpcChatButtons orig, int superColor, Color chatColor, int numLines, string focusText, string focusText2)
	{
		orig(superColor, chatColor, numLines, focusText, focusText2);
		NPC npc = Main.LocalPlayer.TalkNPC;
		if (npc is not null)
			DrawDialogueBubbles(npc, numLines);
	}
}
