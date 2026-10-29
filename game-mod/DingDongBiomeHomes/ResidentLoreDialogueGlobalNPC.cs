using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// First conversation after a resident's quest stage changes: the resident speaks the
/// authored "offer" line of the stage it can give now, or the "waiting" line while a
/// Delayed stage ripens. Later conversations keep the vanilla/mod chat untouched, and
/// chat buttons (Fargo, Calamity, Alchemist...) are never altered: only the text is.
/// </summary>
public sealed class ResidentLoreDialogueGlobalNPC : GlobalNPC
{
	public override void GetChat(NPC npc, ref string chat)
	{
		if (Main.dedServ || !NpcResidentRegistry.TryGet(npc, out string npcKey, out _)) return;
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || ResidentEncounterSystem.IsPending(npcKey) || !LoreCatalog.HasResident(npcKey)) return;
		Player local = Main.LocalPlayer;
		var quest = local.GetModPlayer<NpcQuestPlayer>();
		if (!quest.TryGetCurrent(npcKey, out ResidentQuestProgress progress)) progress = new ResidentQuestProgress();
		int marker = progress.Completed * 2 + (progress.Active ? 1 : 0);
		if (!local.GetModPlayer<QuestLorePlayer>().TryConsumeDialogueChange(npcKey, marker)) return;
		string line = "";
		if (progress.Active && progress.ActiveKind == ResidentQuestKind.Delayed)
			line = LoreCatalog.Waiting(npcKey, progress.ActiveVariant);
		else if (!progress.Active && !PersonalResidentQuestCatalog.Complete(npcKey, progress.Completed))
			line = LoreCatalog.Offer(npcKey, progress.Completed);
		if (line.Length > 0) chat = line;
	}
}
