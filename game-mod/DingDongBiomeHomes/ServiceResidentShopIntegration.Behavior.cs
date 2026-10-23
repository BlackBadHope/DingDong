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
	public static void InstallHooks()
	{
		MethodInfo initialize = typeof(NPCShopDatabase).GetMethod(nameof(NPCShopDatabase.Initialize), BindingFlags.Public | BindingFlags.Static, binder: null, Type.EmptyTypes, modifiers: null) ?? throw new MissingMethodException(typeof(NPCShopDatabase).FullName, nameof(NPCShopDatabase.Initialize));
		MonoModHooks.Add(initialize, (HookInitializeShops)InitializeShops);
		if (Main.dedServ)
			return;
		MethodInfo drawButtons = typeof(Main).GetMethod("DrawNPCChatButtons", BindingFlags.NonPublic | BindingFlags.Static, binder: null, new[] { typeof(int), typeof(Color), typeof(int), typeof(string), typeof(string) }, modifiers: null) ?? throw new MissingMethodException(typeof(Main).FullName, "DrawNPCChatButtons");
		MonoModHooks.Add(drawButtons, (HookDrawNpcChatButtons)DrawNpcChatButtons);
	}

	public static bool IsServiceResident(int npcType) => ServiceResidentsByType.ContainsKey(npcType);
}
