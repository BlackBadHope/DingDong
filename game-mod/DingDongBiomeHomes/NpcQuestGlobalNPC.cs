using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed class NpcQuestGlobalNPC : GlobalNPC
{
	public override void OnKill(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (NpcResidentRegistry.TryGet(npc, out string residentKey, out _))
			NpcResidenceWorldSystem.HandleResidentDeath(residentKey);
		// Bosses can legitimately have zero contact damage in a transition/death
		// state (Queen Slime does this). Their kill credit must not be discarded.
		if (npc.friendly || npc.townNPC || npc.lifeMax <= 5 || !npc.boss && npc.damage <= 0)
			return;
		int playerIndex = npc.lastInteraction;
		if (npc.boss)
		{
			bool creditedParticipant = false;
			for (int index = 0; index < Main.maxPlayers; index++)
			{
				Player participant = Main.player[index];
				if (!participant.active || (index != playerIndex && !npc.playerInteraction[index]))
					continue;
				NpcQuestSystem.RecordNpcKill(participant, npc);
				creditedParticipant = true;
			}

			if (!creditedParticipant && playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex].active)
				NpcQuestSystem.RecordNpcKill(Main.player[playerIndex], npc);
			QuestWorldProgressSystem.RecordBoss(npc);
			return;
		}

		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
			return;
		NpcQuestSystem.RecordNpcKill(Main.player[playerIndex], npc);
	}
}
