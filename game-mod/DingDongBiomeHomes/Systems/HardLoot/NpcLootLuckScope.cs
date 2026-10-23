using System;
using Terraria;

namespace DingDongBiomeHomes.Systems.HardLoot;

// Neutralize luck only while an NPC's loot executes. Fishing, crafting and
// other luck mechanics keep their normal behavior; player luck is never edited.
internal static class NpcLootLuckScope
{
	[ThreadStatic] private static int depth;
	internal static bool Active => depth > 0;
	internal static void Install() => On_Player.RollLuck += RollLuck;
	internal static void Uninstall() { On_Player.RollLuck -= RollLuck; depth = 0; }
	internal static void Run(Action action)
	{
		depth++;
		try { action(); }
		finally { depth--; }
	}
	private static int RollLuck(On_Player.orig_RollLuck orig, Player player, int range) =>
		Active ? Main.rand.Next(range) : orig(player, range);
}
