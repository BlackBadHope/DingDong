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
	private delegate void OrigInitializeShops();
	private delegate void HookInitializeShops(OrigInitializeShops orig);
	private delegate void OrigDrawNpcChatButtons(int superColor, Color chatColor, int numLines, string focusText, string focusText2);
	private delegate void HookDrawNpcChatButtons(OrigDrawNpcChatButtons orig, int superColor, Color chatColor, int numLines, string focusText, string focusText2);
}
