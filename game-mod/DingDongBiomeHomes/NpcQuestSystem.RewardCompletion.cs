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
	private static void CompleteQuestAndGrantReward(Player player, string npcKey, NpcQuestPlayer questPlayer, ResidentQuestProgress progress, int required)
	{
		if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
			return;
		bool unlockedTrust = progress.Completed < required;
		int reward = progress.RewardCopper;
		int rewardDifficulty = progress.DifficultyRank;
		progress.Completed = Math.Min(int.MaxValue - 1, progress.Completed + 1);
		progress.ClearActive();
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		int totalCompleted = questPlayer.CurrentCompletedTotal();
		QuestRewardReceipt receipt = QuestRewardBalance.Grant(player, identity, reward, rewardDifficulty, totalCompleted);
		Notify(player, "Mods.DingDongBiomeHomes.Quests.RewardGranted", QuestRewardBalance.FormatCoins(receipt.CopperValue), receipt.CacheCount, Lang.GetItemNameValue(receipt.CacheItemType), receipt.ThemedItemCount, Lang.GetItemNameValue(receipt.ThemedItemType));
		if (receipt.MilestoneBonus)
			Notify(player, "Mods.DingDongBiomeHomes.Quests.RewardStreak", totalCompleted);
		if (unlockedTrust && progress.Completed >= required)
			Notify(player, "Mods.DingDongBiomeHomes.Quests.TrustReady", progress.Completed, required);
		else if (progress.Completed < required)
			Notify(player, "Mods.DingDongBiomeHomes.Quests.QuestComplete", progress.Completed, required);
		else
			Notify(player, "Mods.DingDongBiomeHomes.Quests.QuestCompleteInfinite", progress.Completed);
		NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}
}
