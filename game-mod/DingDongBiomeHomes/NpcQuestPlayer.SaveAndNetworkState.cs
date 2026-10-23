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
	public override void LoadData(TagCompound tag)
	{
		progressByWorldAndNpc.Clear();
		Revision = 0;
		PersistentId = tag.GetString("PersistentId");
		if (PersistentId.Length == 0)
			PersistentId = Guid.NewGuid().ToString("N");
		identityConfirmed = Main.netMode == NetmodeID.SinglePlayer;
		nextIdentityRetryTick = 0;
		if (!tag.ContainsKey("ResidentQuests"))
			return;
		int version = tag.GetInt("ResidentQuestPlayerVersion");
		foreach (TagCompound entry in tag.GetList<TagCompound>("ResidentQuests"))
		{
			string worldKey = entry.GetString("World");
			string npcKey = entry.GetString("Npc");
			if (worldKey.Length == 0 || !NpcTownManifest.TryGet(npcKey, out _))
				continue;
			bool active = entry.GetBool("Active");
			int completed = Math.Max(0, entry.GetInt("Completed"));
			int activeQuestId = entry.ContainsKey("ActiveQuestId") ? entry.GetInt("ActiveQuestId") : active ? ResidentQuestCatalog.LegacyQuestId(completed) : -1;
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
				loaded.NormalizeLoaded(loadedIdentity, PersistentId);
			progressByWorldAndNpc[ComposeKey(worldKey, npcKey)] = loaded;
		}
	}

	internal void ReplaceCurrentNetworkSnapshot(IEnumerable<(string NpcKey, ResidentQuestProgress Progress)> records, uint revision)
	{
		if (revision < Revision)
			return;
		ReplaceCurrentSnapshot(records);
		Revision = revision;
	}

	internal void SetNetworkIdentity(string persistentId)
	{
		PersistentId = persistentId;
		identityConfirmed = true;
	}

	internal void ConfirmNetworkIdentity() => identityConfirmed = true;
}
