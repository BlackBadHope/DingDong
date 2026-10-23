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
	internal static bool TryGetEscortBubbleText(NPC npc, Player player, out string text)
	{
		text = string.Empty;
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity) || !SettlementWorldState.TryGetResidence(npcKey, out _))
		{
			return false;
		}

		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (state.Stage == ResidentEscortStage.AwaitingHome)
		{
			text = string.Equals(state.OwnerId, questPlayer.PersistentId, StringComparison.Ordinal)
				? Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAwaitingHome")
				: Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBusy");
			return true;
		}
		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting)
		{
			text = state.OwnerId == questPlayer.PersistentId ? Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonFollowing") : Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBusy");
			return true;
		}

		if (state.Stage == ResidentEscortStage.Arrived || progress.Active || progress.Completed < ResidentTrustPolicy.RequiredQuests(identity))
		{
			return false;
		}

		text = Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonInvite");
		return true;
	}
}
