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
internal static partial class NpcQuestSystem
{
	internal static void RequestDialogueInteraction(NPC npc, Player player, bool requestEscort)
	{
		if (npc == null || player == null || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out _, out _))
		{
			return;
		}

		NpcQuestNetwork.RequestQuestInteraction(npc.whoAmI, requestEscort);
		Main.npcChatText = Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.RequestSent");
	}
}
