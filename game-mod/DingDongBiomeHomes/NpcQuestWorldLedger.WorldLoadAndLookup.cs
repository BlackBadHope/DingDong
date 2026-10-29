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
	public override void LoadWorldData(TagCompound tag)
	{
		Records.Clear();
		LoadShared(tag);
		int version = tag.GetInt("ResidentQuestLedgerVersion");
		if ((version < 1 || version > SaveVersion) || !tag.ContainsKey("ResidentQuestLedger"))
		{
			ReconcileLoadedChoices();
			return;
		}
		foreach (TagCompound entry in tag.GetList<TagCompound>("ResidentQuestLedger"))
		{
			string ownerId = entry.GetString("OwnerId");
			string npcKey = entry.GetString("NpcKey");
			if (!Guid.TryParseExact(ownerId, "N", out _) || !NpcTownManifest.TryGet(npcKey, out _))
				continue;
			bool active = entry.GetBool("Active");
			int completed = Math.Max(0, entry.GetInt("Completed"));
			int activeQuestId = version >= 2 ? entry.GetInt("ActiveQuestId") : active ? ResidentQuestCatalog.LegacyQuestId(completed) : -1;
			bool frozenDefinition = version >= 3 && entry.GetInt("ActiveGoal") > 0;
			if (activeQuestId < 0 || (!frozenDefinition && activeQuestId >= ResidentQuestCatalog.PoolSize) || activeQuestId > 1_000_000)
			{
				active = false;
				activeQuestId = -1;
			}

			ResidentQuestProgress loaded = new()
			{
				Completed = completed,
				Active = active,
				Progress = Math.Max(0, entry.GetInt("Progress")),
				ActiveQuestId = activeQuestId,
				ActiveKind = version >= 3 ? (ResidentQuestKind)entry.GetInt("ActiveKind") : default,
				ActiveVariant = version >= 3 ? entry.GetInt("ActiveVariant") : 0,
				ActiveItemType = version >= 3 ? entry.GetInt("ActiveItemType") : ItemID.None,
				ActiveGoal = version >= 3 ? Math.Max(0, entry.GetInt("ActiveGoal")) : 0,
				ActiveRadiusTiles = version >= 3 ? Math.Max(0, entry.GetInt("ActiveRadiusTiles")) : 0,
				ActiveTargetKey = version >= 3 ? entry.GetString("ActiveTargetKey") : string.Empty,
				ActiveTargetName = version >= 3 ? entry.GetString("ActiveTargetName") : string.Empty,
				RewardCopper = version >= 3 ? Math.Max(0, entry.GetInt("RewardCopper")) : 0,
				DifficultyRank = version >= 3 ? Math.Clamp(entry.GetInt("DifficultyRank"), 0, 3) : 0
			};
			if (loaded.Active && loaded.ActiveGoal <= 0 && NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
			{
				ResidentQuestDefinition scaled = QuestRewardBalance.Scale(ResidentQuestCatalog.Resolve(identity, activeQuestId), out int reward, out int difficulty);
				int oldProgress = loaded.Progress;
				loaded.Begin(scaled, reward, difficulty);
				loaded.Progress = Math.Min(oldProgress, scaled.Goal);
			}

			if (loaded.Active && NpcTownManifest.TryGet(npcKey, out NpcTownIdentity loadedIdentity))
				loaded.NormalizeLoaded(loadedIdentity, ownerId);
			Records[Compose(ownerId, npcKey)] = loaded;
		}
		MigrateLegacyRecords();
		ReconcileLoadedChoices();
	}

	internal static bool TryRead(string ownerId, string npcKey, out ResidentQuestProgress progress)
	{
		if (Shared.TryGetValue(npcKey, out ResidentQuestProgress stored) || Records.TryGetValue(Compose(ownerId, npcKey), out stored))
		{
			progress = stored.Clone();
			return true;
		}

		progress = null;
		return false;
	}
}
