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
internal sealed class ResidentQuestProgress
{
	internal int Completed { get; set; }
	internal bool Active { get; set; }
	internal int Progress { get; set; }
	internal int ActiveQuestId { get; set; } = -1;
	internal ResidentQuestKind ActiveKind { get; set; }
	internal int ActiveVariant { get; set; }
	internal int ActiveItemType { get; set; }
	internal int ActiveGoal { get; set; }
	internal int ActiveRadiusTiles { get; set; }
	internal string ActiveTargetKey { get; set; } = string.Empty;
	internal string ActiveTargetName { get; set; } = string.Empty;
	internal int RewardCopper { get; set; }
	internal int DifficultyRank { get; set; }

	internal ResidentQuestProgress Clone() => new()
	{
		Completed = Completed,
		Active = Active,
		Progress = Progress,
		ActiveQuestId = ActiveQuestId,
		ActiveKind = ActiveKind,
		ActiveVariant = ActiveVariant,
		ActiveItemType = ActiveItemType,
		ActiveGoal = ActiveGoal,
		ActiveRadiusTiles = ActiveRadiusTiles,
		ActiveTargetKey = ActiveTargetKey,
		ActiveTargetName = ActiveTargetName,
		RewardCopper = RewardCopper,
		DifficultyRank = DifficultyRank
	};
	internal void Begin(ResidentQuestDefinition definition, int rewardCopper, int difficultyRank)
	{
		Active = true;
		Progress = 0;
		ActiveQuestId = definition.Id;
		ActiveKind = definition.Kind;
		ActiveVariant = definition.Variant;
		ActiveItemType = definition.ItemType;
		ActiveGoal = definition.Goal;
		ActiveRadiusTiles = definition.RadiusTiles;
		ActiveTargetKey = definition.TargetKey ?? string.Empty;
		ActiveTargetName = definition.TargetName ?? string.Empty;
		RewardCopper = rewardCopper;
		DifficultyRank = difficultyRank;
	}

	internal ResidentQuestDefinition Definition(NpcTownIdentity identity)
	{
		if (ActiveGoal > 0)
		{
			return new ResidentQuestDefinition(ActiveQuestId, ActiveKind, ActiveVariant, ActiveItemType, ActiveGoal, ActiveRadiusTiles, ActiveTargetKey, ActiveTargetName, RewardCopper);
		}

		ResidentQuestDefinition legacy = ResidentQuestCatalog.Resolve(identity, ActiveQuestId);
		return QuestRewardBalance.Scale(legacy, out _, out _);
	}

	internal void ClearActive()
	{
		Active = false;
		Progress = 0;
		ActiveQuestId = -1;
		ActiveGoal = 0;
		ActiveTargetKey = string.Empty;
		ActiveTargetName = string.Empty;
		RewardCopper = 0;
		DifficultyRank = 0;
	}

	internal bool NormalizeLoaded(NpcTownIdentity identity, string persistentId)
	{
		if (!Active)
			return false;
		if (ActiveGoal > 0 && Enum.IsDefined(ActiveKind))
		{
			ResidentQuestDefinition saved = Definition(identity);
			if (Progress >= saved.Goal)
				return false;
			if (QuestContentRegistry.TryRebind(saved, out ResidentQuestDefinition rebound) && QuestContentRegistry.TargetExists(rebound))
			{
				bool changed = rebound.ItemType != ActiveItemType || !string.Equals(rebound.TargetName, ActiveTargetName, StringComparison.Ordinal);
				ActiveItemType = rebound.ItemType;
				ActiveTargetName = rebound.TargetName ?? string.Empty;
				Progress = Math.Min(Progress, ActiveGoal);
				return changed;
			}
		}

		ResidentQuestDefinition selected = ResidentQuestCatalog.SelectAvailable(persistentId, identity, Completed);
		ResidentQuestDefinition scaled = QuestRewardBalance.Scale(selected, out int rewardCopper, out int difficultyRank);
		Begin(scaled, rewardCopper, difficultyRank);
		return true;
	}
}
