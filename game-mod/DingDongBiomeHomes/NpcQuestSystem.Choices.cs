using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
	// Dialogue-Choice form: the assigned performer picks one authored option while
	// standing at the resident. The pick itself lives in the world ledger (choice id);
	// Progress only says "answered" (1 of Goal 1), so snapshots and saves stay within
	// the Progress <= Goal contract whichever option was taken.
	internal static void HandleChoice(Player player, int npcIndex, int choice)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || player is null || !player.active || player.dead ||
			!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || npcIndex < 0 || npcIndex >= Main.maxNPCs)
			return;
		NPC npc = Main.npc[npcIndex];
		if (!npc.active || Vector2.DistanceSquared(player.Center, npc.Center) > 14f * 16f * 14f * 16f || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity))
		{
			Notify(player, "Mods.DingDongBiomeHomes.Quests.InvalidResident");
			return;
		}
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		if (!progress.Active || !progress.IsAssignedTo(questPlayer.PersistentId)) return;
		ResidentQuestDefinition definition = progress.Definition(identity);
		if (definition.Kind != ResidentQuestKind.DialogueChoice || progress.Progress > 0) return;
		var step = PersonalResidentQuestCatalog.Step(definition.StoryKey);
		if (step == null || choice < 0 || choice >= step.Choices.Length) return;
		NpcQuestWorldLedger.RecordChoice(npcKey, definition.Variant, step.Choices[choice].Id);
		if (!NpcQuestWorldLedger.TryGetChoice(npcKey, definition.Variant, out string recorded) || recorded != step.Choices[choice].Id) return;
		progress.Progress = 1;
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		QuestNotice.Send(player, QuestNoticeKind.Chosen, npcKey, definition.Variant, choice);
		NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}

	// Abandon: the performer drops the active stage. Earned trust (Completed) and the
	// choices of completed stages stay; accepting again restarts the stage from zero.
	internal static void HandleAbandon(Player player, string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || player is null || !player.active ||
			!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcTownManifest.TryGet(npcKey, out _))
			return;
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (!questPlayer.IdentityConfirmed) return;
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		if (!progress.Active || !(progress.IsAssignedTo(questPlayer.PersistentId) || progress.IsPausedFor(questPlayer.PersistentId))) return;
		int stage = progress.ActiveVariant;
		if (progress.ActiveKind == ResidentQuestKind.DialogueChoice) NpcQuestWorldLedger.ForgetChoice(npcKey, stage);
		progress.ClearActive();
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		QuestNotice.Send(player, QuestNoticeKind.Abandoned, npcKey, stage);
		NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}
}
