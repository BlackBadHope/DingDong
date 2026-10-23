using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class NpcEscortMobilityGlobalNPC
{
	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		lastPosition = npc.position;
		if (Main.netMode != NetmodeID.MultiplayerClient && NpcResidentRegistry.TryGet(npc, out string npcKey, out _))
		{
			NpcResidenceWorldSystem.ApplyLockedHome(npc, npcKey);
		}
	}
}
