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
public sealed partial class NpcQuestWorldLedger
{
	public override void OnWorldUnload() { Records.Clear(); ClearShared(); }
	public override void SaveWorldData(TagCompound tag)
	{
		SaveShared(tag);
		tag["ResidentQuestLedgerVersion"] = SaveVersion;
		tag["ResidentQuestLedger"] = Records.Select(pair =>
		{
			Split(pair.Key, out string ownerId, out string npcKey);
			return new TagCompound
			{
				["OwnerId"] = ownerId,
				["NpcKey"] = npcKey,
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
