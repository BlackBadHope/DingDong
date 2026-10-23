using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class DevianttGiftPolicy
{
	private const string GiftSource = "DingDongDevianttCheckPulse";
	private delegate void OrigDropDevianttsGift(Player player);

	private static Hook hook;
	private static int eternityAdvisorType;

	internal static bool Installed => hook is not null;

	internal static void Install()
	{
		if (Installed) return;

		Mod provider = HardLootProviderGate.Require("FargowiltasSouls", new Version(1, 7, 3, 9),
			"535d0777-d3df-4436-ac3f-e1d1fd0ad83b");
		if (!provider.TryFind("EternityAdvisor", out ModItem advisor))
			throw new InvalidOperationException("FargowiltasSouls EternityAdvisor item is unavailable.");

		Type type = provider.Code.GetType("FargowiltasSouls.FargowiltasSouls", true);
		MethodInfo target = type.GetMethod("DropDevianttsGift", BindingFlags.Public | BindingFlags.Static,
			null, [typeof(Player)], null);
		MethodInfo replacement = typeof(DevianttGiftPolicy).GetMethod(nameof(DropAdvisorOnly),
			BindingFlags.NonPublic | BindingFlags.Static);
		if (target?.ReturnType != typeof(void) || replacement is null)
			throw new InvalidOperationException("FargowiltasSouls DropDevianttsGift(Player) surface changed.");

		eternityAdvisorType = advisor.Type;
		hook = new Hook(target, replacement);
	}

	internal static void Uninstall()
	{
		hook?.Dispose();
		hook = null;
		eternityAdvisorType = 0;
	}

	private static void DropAdvisorOnly(OrigDropDevianttsGift orig, Player player)
	{
		if (player is null || !player.active || eternityAdvisorType <= ItemID.None)
			return;

		Item.NewItem(player.GetSource_Misc(GiftSource), player.Center, eternityAdvisorType);
	}
}
