using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestSystem
{
	// Dialogue-Choice form: the assigned performer picks one authored option while
	// standing at the resident; the pick (1-based index) is the shared progress and
	// is recorded in the world ledger, so the chain stays finite and replay-safe.
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
		progress.Progress = choice + 1;
		NpcQuestWorldLedger.Record(player, npcKey, progress);
		Notify(player, "Mods.DingDongBiomeHomes.SharedQuests.Chosen", step.Choices[choice].Text);
		NpcQuestNetwork.SendQuestSnapshot(player, player.whoAmI);
	}
}
