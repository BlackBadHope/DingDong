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
			ResidentQuestKind.DeliverSupplies => CountInventory(player, definition.ItemType) >= definition.Goal,
			ResidentQuestKind.BannerCollection => CountInventory(player, definition.ItemType) > 0,
			ResidentQuestKind.ModProgression when definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) => progress.Progress >= definition.Goal || QuestContentRegistry.IsExternalMilestoneComplete(definition.TargetKey),
			_ => progress.Progress >= definition.Goal
		};
	}
}
