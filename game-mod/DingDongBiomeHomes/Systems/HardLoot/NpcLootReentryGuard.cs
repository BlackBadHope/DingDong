using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class NpcLootReentryGuard
{
	[System.ThreadStatic]
	private static HashSet<NPC> activeCalls;
	internal static bool Installed { get; private set; }
	internal static bool IsOuterCallActive(NPC npc) => activeCalls?.Contains(npc) == true;

	internal static void Install()
	{
		if (Installed) return;
		On_NPC.NPCLoot += NpcLoot;
		try { NpcLootLuckScope.Install(); }
		catch { On_NPC.NPCLoot -= NpcLoot; throw; }
		Installed = true;
	}

	internal static void Uninstall()
	{
		if (!Installed) return;
		On_NPC.NPCLoot -= NpcLoot;
		NpcLootLuckScope.Uninstall();
		activeCalls?.Clear();
		Installed = false;
	}

	private static void NpcLoot(On_NPC.orig_NPCLoot orig, NPC npc)
	{
		activeCalls ??= [];
		if (activeCalls.Contains(npc))
		{
			HardLootAudit.ProviderReplayBlocked("recursive-NPCLoot");
			return;
		}
		HardLootRuntimeGuard.EnforceBeforeNpcLoot();
		activeCalls.Add(npc);
		try { NpcLootLuckScope.Run(() => orig(npc)); }
		finally { activeCalls.Remove(npc); }
	}
}
