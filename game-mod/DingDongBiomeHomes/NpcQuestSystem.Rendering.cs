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
	private static string SurveyTargetLabel(int variant)
	{
		string suffix = SurveyTargetKeys[Math.Clamp(variant, 0, SurveyTargetKeys.Length - 1)];
		return Language.GetTextValue($"Mods.DingDongBiomeHomes.Quests.SurveyTargets.{suffix}");
	}

	private static string PatrolDirectionLabel(int pointIndex)
	{
		string suffix = PatrolDirectionKeys[Math.Clamp(pointIndex, 0, PatrolDirectionKeys.Length - 1)];
		return Language.GetTextValue($"Mods.DingDongBiomeHomes.Quests.PatrolDirections.{suffix}");
	}
}
