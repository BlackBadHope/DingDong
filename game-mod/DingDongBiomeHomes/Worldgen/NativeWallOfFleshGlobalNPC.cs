using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeWallOfFleshGlobalNPC : GlobalNPC
{
	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		// Covers direct/admin/mod spawns which bypass NPC.SpawnWOF. Clients derive
		// the same corridor from the synchronized boss position.
		if (npc.type == NPCID.WallofFlesh)
			NativeUnderworldSystem.ObserveWallOfFlesh(npc.Center.Y);
	}
}
