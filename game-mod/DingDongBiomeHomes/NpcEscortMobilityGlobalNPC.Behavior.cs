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
	public override bool PreAI(NPC npc)
	{
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out string npcKey, out _))
		{
			return true;
		}

		NpcResidenceWorldSystem.ApplyLockedHome(npc, npcKey);
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (state.Stage is not (ResidentEscortStage.Following or ResidentEscortStage.Waiting))
			return true;
		RunEscortAI(npc, npcKey, state);
		return false;
	}

	public override void PostAI(NPC npc)
	{
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out string npcKey, out _))
		{
			return;
		}

		NpcResidenceWorldSystem.ApplyLockedHome(npc, npcKey);
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting)
			return;
		// Preserve every mod's town AI. Only assist when it is demonstrably walking into an obstacle.
		bool tryingToWalk = Math.Abs(npc.velocity.X) > 0.12f;
		bool moved = Math.Abs(npc.position.X - lastPosition.X) > 0.35f;
		if (tryingToWalk && !moved && npc.collideX)
			vanillaStuckTicks++;
		else
			vanillaStuckTicks = Math.Max(0, vanillaStuckTicks - 2);
		if (vanillaStuckTicks == 24)
			TryOpenDoor(npc, Math.Sign(npc.velocity.X));
		if (vanillaStuckTicks >= 38 && npc.collideY)
		{
			npc.velocity.Y = -5.4f;
			vanillaStuckTicks = 0;
			npc.netUpdate = true;
		}

		lastPosition = npc.position;
	}
}
