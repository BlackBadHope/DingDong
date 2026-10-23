using Terraria;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class LivingNpcLootReplayGuard
{
	internal static bool Installed { get; private set; }

	internal static void Install()
	{
		if (Installed) return;
		On_NPC.NPCLoot_DropItems += DropItems;
		On_NPC.NPCLoot_DropMoney += DropMoney;
		On_NPC.NPCLoot_DropHeals += DropHeals;
		Installed = true;
	}

	internal static void Uninstall()
	{
		if (!Installed) return;
		On_NPC.NPCLoot_DropItems -= DropItems;
		On_NPC.NPCLoot_DropMoney -= DropMoney;
		On_NPC.NPCLoot_DropHeals -= DropHeals;
		Installed = false;
	}

	private static void DropItems(On_NPC.orig_NPCLoot_DropItems orig, NPC npc, Player player)
	{
		if (Reject(npc, "items")) return;
		NpcLootLuckScope.Run(() => orig(npc, player));
	}

	private static void DropMoney(On_NPC.orig_NPCLoot_DropMoney orig, NPC npc, Player player)
	{
		if (Reject(npc, "money")) return;
		orig(npc, player);
	}

	private static void DropHeals(On_NPC.orig_NPCLoot_DropHeals orig, NPC npc, Player player)
	{
		if (Reject(npc, "heals")) return;
		orig(npc, player);
	}

	private static bool Reject(NPC npc, string surface)
	{
		if (npc is null || !npc.active || npc.life <= 0 || NpcLootReentryGuard.IsOuterCallActive(npc))
			return false;
		HardLootAudit.LivingReplayBlocked(surface);
		return true;
	}
}
