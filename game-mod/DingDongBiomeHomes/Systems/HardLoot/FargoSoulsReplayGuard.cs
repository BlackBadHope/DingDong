using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class FargoSoulsReplayGuard
{
	private static Hook hook;
	internal static bool Installed => hook is not null;

	internal static void Install()
	{
		if (Installed) return;
		Mod provider = HardLootProviderGate.Require("FargowiltasSouls", new Version(1, 7, 3, 9),
			"535d0777-d3df-4436-ac3f-e1d1fd0ad83b");
		Type type = provider.Code.GetType("FargowiltasSouls.Core.Globals.FargoSoulsGlobalNPC", true);
		MethodInfo target = type.GetMethod("DropLoot", BindingFlags.Public | BindingFlags.Static,
			null, [typeof(NPC)], null);
		MethodInfo replacement = typeof(FargoSoulsReplayGuard).GetMethod(nameof(BlockReplay),
			BindingFlags.NonPublic | BindingFlags.Static);
		if (target?.ReturnType != typeof(void) || replacement is null)
			throw new InvalidOperationException("FargowiltasSouls DropLoot(NPC) surface changed.");
		hook = new Hook(target, replacement);
	}

	internal static void Uninstall()
	{
		hook?.Dispose();
		hook = null;
	}

	private static void BlockReplay(NPC npc) => HardLootAudit.ProviderReplayBlocked("FargowiltasSouls");
}
