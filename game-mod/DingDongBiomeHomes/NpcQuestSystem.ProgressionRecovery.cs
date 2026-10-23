using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

internal static partial class NpcQuestSystem
{
	private static bool RepairUnavailableProgression(Player player, string npcKey, NpcTownIdentity identity, ResidentQuestProgress progress)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !progress.Active ||
			progress.ActiveKind != ResidentQuestKind.ModProgression || progress.Progress >= progress.ActiveGoal ||
			progress.ActiveTargetKey.StartsWith("milestone:", StringComparison.Ordinal) ||
			QuestContentRegistry.TargetExists(progress.Definition(identity)))
			return false;
		// Another player can consume the last first-kill target in this mod.
		// Replace the impossible objective without awarding trust or changing
		// the reward already promised for this active quest.
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (!questPlayer.IdentityConfirmed)
			return false;
		ResidentQuestDefinition selected = ResidentQuestCatalog.SelectAvailable(questPlayer.PersistentId, identity, progress.Completed,
			allowResidenceObjectives: SettlementWorldState.TryGetResidence(npcKey, out _));
		ResidentQuestDefinition scaled = QuestRewardBalance.Scale(selected, out _, out _);
		int reward = progress.RewardCopper;
		int difficulty = progress.DifficultyRank;
		progress.Begin(scaled, reward, difficulty);
		NotifyQuestAccepted(player, scaled, identity);
		return true;
	}
}
