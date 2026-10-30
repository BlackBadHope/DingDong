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
		if (!progress.Active || !progress.IsAssignedTo(questPlayer.PersistentId)) return;
		ResidentQuestDefinition definition = progress.Definition(identity);
		int completedQuest = progress.ActiveQuestId;
		bool unlockedTrust = progress.Completed < required;
		int reward = ResidentReputationSystem.ScaleReward(questPlayer.PersistentId, npcKey, progress.RewardCopper);
		int rewardDifficulty = progress.DifficultyRank;
		if (definition.Kind == ResidentQuestKind.DialogueChoice && definition.StoryKey.Length > 0)
		{
			// The pick was recorded when it was made and outlives the stage: it is the world's
			// answer for the codex and finales. A stage without a recorded pick is reset on
			// load (ReconcileLoadedChoices); the marker below only guards odd states.
			if (!NpcQuestWorldLedger.TryGetChoice(npcKey, definition.Variant, out _))
				NpcQuestWorldLedger.RecordChoice(npcKey, definition.Variant, NpcQuestWorldLedger.LegacyChoice);
		}
		progress.Completed = Math.Min(int.MaxValue - 1, progress.Completed + 1);
		progress.ClearActive();
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		ResidentReputationSystem.OnQuestCompleted(player, npcKey, progress.Completed, PersonalResidentQuestCatalog.Length(npcKey));
		int totalCompleted = questPlayer.CurrentCompletedTotal();
		QuestRewardReceipt receipt = definition.StoryKey.Length > 0 ? QuestRewardBalance.GrantPersonal(player, definition, reward) : QuestRewardBalance.Grant(player, identity, reward, rewardDifficulty, totalCompleted);
		NpcQuestWorldLedger.RecordCompletion(npcKey, completedQuest, player, receipt);
		if (definition.StoryKey.Length > 0) QuestNotice.Send(player, QuestNoticeKind.Completed, npcKey, definition.Variant, reward);
        else Notify(player, "Mods.DingDongBiomeHomes.Quests.RewardGranted", QuestRewardBalance.FormatCoins(receipt.CopperValue), receipt.CacheCount, Lang.GetItemNameValue(receipt.CacheItemType), receipt.ThemedItemCount, Lang.GetItemNameValue(receipt.ThemedItemType));
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
