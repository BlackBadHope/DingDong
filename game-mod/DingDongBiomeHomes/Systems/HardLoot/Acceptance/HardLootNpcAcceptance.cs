using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot.Acceptance;

internal static class HardLootNpcAcceptance
{
	internal static int Run(Player player)
	{
		(int Npc, int Item)[] cases = [(NPCID.BlueSlime, ItemID.Gel),
			(NPCID.KingSlime, ItemID.KingSlimeBossBag),
			(NPCID.EyeofCthulhu, ItemID.EyeOfCthulhuBossBag)];
		int slot = Main.maxNPCs - 1;
		while (slot >= 0 && Main.npc[slot].active) slot--;
		if (slot < 0) throw new InvalidOperationException("No empty NPC slot for loot acceptance.");
		NPC previous = Main.npc[slot];
		int checks = 0;
		try
		{
			foreach (var entry in cases)
			for (int repeat = 0; repeat < 3; repeat++)
			{
				NPC npc = new();
				npc.SetDefaults(entry.Npc);
				npc.whoAmI = slot;
				npc.position = player.position;
				npc.life = 0;
				npc.active = false;
				npc.playerInteraction[player.whoAmI] = true;
				Main.npc[slot] = npc;
				var info = new DropAttemptInfo { npc = npc, player = player,
					IsExpertMode = true, IsMasterMode = true, rng = Main.rand };
				var actual = HardLootAcceptanceCapture.Observe(
					() => Main.ItemDropSolver.TryDropping(info));
				if (!actual.TryGetValue(entry.Item, out int count) || count < 1)
					throw new InvalidOperationException(
						$"NPC {entry.Npc} resolver lost guaranteed item {entry.Item}.");
				checks++;
			}
		}
		finally { Main.npc[slot] = previous; }
		return checks;
	}
}
