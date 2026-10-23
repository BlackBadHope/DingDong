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
public sealed partial class NpcQuestPlayer
{
	public override void SaveData(TagCompound tag)
	{
		tag["PersistentId"] = PersistentId;
		tag["ResidentQuestPlayerVersion"] = 4;
		tag["ResidentQuests"] = progressByWorldAndNpc.Select(pair =>
		{
			SplitKey(pair.Key, out string worldKey, out string npcKey);
			return new TagCompound
			{
				["World"] = worldKey,
				["Npc"] = npcKey,
				["Completed"] = pair.Value.Completed,
				["Active"] = pair.Value.Active,
				["Progress"] = pair.Value.Progress,
				["ActiveQuestId"] = pair.Value.ActiveQuestId,
				["ActiveKind"] = (int)pair.Value.ActiveKind,
				["ActiveVariant"] = pair.Value.ActiveVariant,
				["ActiveItemType"] = pair.Value.ActiveItemType,
				["ActiveGoal"] = pair.Value.ActiveGoal,
				["ActiveRadiusTiles"] = pair.Value.ActiveRadiusTiles,
				["ActiveTargetKey"] = pair.Value.ActiveTargetKey,
				["ActiveTargetName"] = pair.Value.ActiveTargetName,
				["RewardCopper"] = pair.Value.RewardCopper,
				["DifficultyRank"] = pair.Value.DifficultyRank
			};
		}).ToList();
	}
}
