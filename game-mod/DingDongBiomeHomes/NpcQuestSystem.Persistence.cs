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
	internal static bool IsReady(Player player, ResidentQuestDefinition definition, ResidentQuestProgress progress)
	{
		return definition.Kind switch
		{
			ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition => QuestInventoryCount(player,definition,progress) >= definition.Goal,
			ResidentQuestKind.BannerCollection => QuestInventoryCount(player,definition,progress) > 0,
			ResidentQuestKind.ModProgression when definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) => progress.Progress >= definition.Goal || QuestContentRegistry.IsExternalMilestoneComplete(definition.TargetKey),
			_ => progress.Progress >= definition.Goal
		};
	}
    internal static int QuestInventoryCount(Player viewer, ResidentQuestDefinition definition, ResidentQuestProgress progress) =>
        progress.IsAssignedTo(viewer.GetModPlayer<NpcQuestPlayer>().PersistentId) ? CountQuestSupplies(viewer,definition) : progress.Progress;

}
