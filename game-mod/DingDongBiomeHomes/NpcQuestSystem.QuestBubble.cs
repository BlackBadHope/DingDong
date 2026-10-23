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
	internal static bool TryGetQuestBubbleText(NPC npc, Player player, out string text)
	{
		text = string.Empty;
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort || !NpcResidentRegistry.TryGet(npc, out string npcKey, out NpcTownIdentity identity))
		{
			return false;
		}

		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		ResidentQuestProgress progress = questPlayer.Get(npcKey);
		int required = ResidentTrustPolicy.RequiredQuests(identity);
		bool hasResidence = SettlementWorldState.TryGetResidence(npcKey, out _);
		ResidentEscortState state = NpcResidenceWorldSystem.GetState(npcKey);
		if (state.Stage is ResidentEscortStage.Following or ResidentEscortStage.Waiting or ResidentEscortStage.AwaitingHome)
			return false;
		if (!progress.Active || !hasResidence && UsesResidenceMap(progress.Definition(identity).Kind))
		{
			text = progress.Completed < required ? Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAccept", progress.Completed + 1, required) : Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAcceptContinued", progress.Completed + 1);
			return true;
		}

		ResidentQuestDefinition definition = progress.Definition(identity);
		text = definition.Kind switch
		{
			ResidentQuestKind.SurveyOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.SurveyOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonSurvey", SurveyTargetLabel(definition.Variant)),
			ResidentQuestKind.PatrolOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.PatrolOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonPatrol", progress.Progress, definition.Goal, PatrolDirectionLabel(PatrolPointIndex(definition.Variant, progress.Progress))),
			ResidentQuestKind.DeliverSupplies => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonSupplies", CountInventory(player, definition.ItemType), definition.Goal),
			ResidentQuestKind.DefendOrigin when progress.Progress >= definition.Goal => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.DefendOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonDefense", progress.Progress, definition.Goal),
			ResidentQuestKind.BossHunt when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BossHunt => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBoss", definition.TargetName),
			ResidentQuestKind.BiomePurge when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BiomePurge => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBiome", progress.Progress, definition.Goal),
			ResidentQuestKind.BannerCollection when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.BannerCollection => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonBanner", Lang.GetItemNameValue(definition.ItemType)),
			ResidentQuestKind.ModProgression when IsReady(player, definition, progress) => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonTurnIn"),
			ResidentQuestKind.ModProgression => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonProgression", definition.TargetName),
			_ => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ButtonAcceptContinued", progress.Completed + 1)};
		return true;
	}
}
