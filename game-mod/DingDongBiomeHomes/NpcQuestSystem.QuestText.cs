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
	private static void NotifyQuestAccepted(Player player, ResidentQuestDefinition definition, NpcTownIdentity identity)
	{
		switch (definition.Kind)
		{
			case ResidentQuestKind.SurveyOrigin:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.SurveyAccepted", identity.Theme, SurveyTargetLabel(definition.Variant));
				break;
			case ResidentQuestKind.PatrolOrigin:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.PatrolAccepted", identity.Theme, definition.Goal, PatrolDirectionLabel(PatrolPointIndex(definition.Variant, 0)));
				break;
			case ResidentQuestKind.DeliverSupplies:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.SuppliesAccepted", definition.Goal, Lang.GetItemNameValue(definition.ItemType));
				break;
			case ResidentQuestKind.DefendOrigin:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.DefenseAccepted", definition.Goal, definition.RadiusTiles);
				break;
			case ResidentQuestKind.BossHunt:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.BossAccepted", definition.TargetName);
				break;
			case ResidentQuestKind.BiomePurge:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.BiomeAccepted", definition.Goal, definition.TargetName);
				break;
			case ResidentQuestKind.BannerCollection:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.BannerAccepted", Lang.GetItemNameValue(definition.ItemType), definition.TargetName);
				break;
			case ResidentQuestKind.ModProgression:
				Notify(player, "Mods.DingDongBiomeHomes.Quests.ProgressionAccepted", definition.TargetName);
				break;
		}
	}

	internal static string ObjectiveText(Player player, ResidentQuestDefinition definition, ResidentQuestProgress progress)
	{
		return definition.Kind switch
		{
			ResidentQuestKind.SurveyOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Survey", SurveyTargetLabel(definition.Variant)),
			ResidentQuestKind.PatrolOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Patrol", progress.Progress, definition.Goal, PatrolDirectionLabel(PatrolPointIndex(definition.Variant, Math.Min(progress.Progress, definition.Goal - 1)))),
			ResidentQuestKind.DeliverSupplies => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Supplies", Math.Min(CountInventory(player, definition.ItemType), definition.Goal), definition.Goal, Lang.GetItemNameValue(definition.ItemType)),
			ResidentQuestKind.DefendOrigin => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Defense", progress.Progress, definition.Goal),
			ResidentQuestKind.BossHunt => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Boss", definition.TargetName, progress.Progress, definition.Goal),
			ResidentQuestKind.BiomePurge => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Biome", definition.TargetName, progress.Progress, definition.Goal),
			ResidentQuestKind.BannerCollection => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Banner", Lang.GetItemNameValue(definition.ItemType), definition.TargetName, CountInventory(player, definition.ItemType) > 0 ? 1 : 0),
			ResidentQuestKind.ModProgression => Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.Objectives.Progression", definition.TargetName, progress.Progress, definition.Goal),
			_ => definition.TargetName
		};
	}

	internal static string ResidentName(string npcKey)
	{
		return NpcResidentRegistry.TryGetType(npcKey, out int npcType) ? Lang.GetNPCNameValue(npcType) : npcKey;
	}

	internal static string DifficultyName(int difficultyRank) => Language.GetTextValue($"Mods.DingDongBiomeHomes.Quests.Difficulty.{Math.Clamp(difficultyRank, 0, 3) switch {0 => "Classic", 1 => "Expert", 2 => "Master", _ => "Legendary"}}");
}
