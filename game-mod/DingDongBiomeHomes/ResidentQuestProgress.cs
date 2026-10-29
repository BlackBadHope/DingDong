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
	internal string AssignedTo { get; set; } = string.Empty;
	internal string AssignedName { get; set; } = string.Empty;
	internal string LastCompletedBy { get; set; } = string.Empty;
	// A paused task (its performer left the world) belongs to nobody until a player resumes it.
	internal bool IsAssignedTo(string id) => !Paused && AssignedTo.Length > 0 && string.Equals(AssignedTo, id, StringComparison.Ordinal);
	internal bool IsPausedFor(string id) => Paused && AssignedTo.Length > 0 && string.Equals(AssignedTo, id, StringComparison.Ordinal);
	internal bool Paused { get; set; }
	// Delayed form: world quest clock tick at acceptance (NpcQuestWorldLedger.Clock).
	internal long StartClock { get; set; }
	// Gather-Env form: matching tiles that already stood in the area at acceptance.
	internal int Baseline { get; set; }
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
	internal string ActiveStoryKey { get; set; } = string.Empty;
	internal int RewardItemType { get; set; }
	internal int RewardItemCount { get; set; }

	internal ResidentQuestProgress Clone() => new()
	{
		Completed = Completed,
		AssignedTo = AssignedTo, AssignedName = AssignedName, LastCompletedBy = LastCompletedBy, Paused = Paused, StartClock = StartClock, Baseline = Baseline,
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
		DifficultyRank = DifficultyRank, ActiveStoryKey = ActiveStoryKey, RewardItemType = RewardItemType, RewardItemCount = RewardItemCount
	};
	internal void Begin(ResidentQuestDefinition definition, int rewardCopper, int difficultyRank)
	{
		ActiveStoryKey = definition.StoryKey; RewardItemType = definition.RewardItemType; RewardItemCount = definition.RewardItemCount;
		Active = true;
		Paused = false; StartClock = 0; Baseline = 0;
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
			return new ResidentQuestDefinition(ActiveQuestId, ActiveKind, ActiveVariant, ActiveItemType, ActiveGoal, ActiveRadiusTiles, ActiveTargetKey, ActiveTargetName, RewardCopper, ActiveStoryKey, RewardItemType, RewardItemCount);
		}

		ResidentQuestDefinition legacy = ResidentQuestCatalog.Resolve(identity, ActiveQuestId);
		return QuestRewardBalance.Scale(legacy, out _, out _);
	}

	internal void ClearActive()
	{
		ActiveStoryKey = string.Empty; RewardItemType = RewardItemCount = 0;
		Active = false;
		Paused = false; StartClock = 0; Baseline = 0;
		AssignedTo = AssignedName = string.Empty;
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
        if (!Active) return false;
        if (ActiveStoryKey.Length == 0 || !PersonalResidentQuestCatalog.TryResolve(ActiveStoryKey, out var rebound))
        { ClearActive(); return true; } // Retire old random tasks without consuming supplies or changing earned trust.
        // A stage rewritten since the save (other form or target) cannot continue its old progress:
        // retire it the same way, so the resident offers the same stage again with its new definition.
        if (rebound.Kind != ActiveKind || !string.Equals(rebound.TargetKey, ActiveTargetKey, StringComparison.Ordinal))
        { ClearActive(); return true; }
        bool changed = rebound.ItemType != ActiveItemType || rebound.RewardItemType != RewardItemType || rebound.TargetName != ActiveTargetName;
        ActiveItemType = rebound.ItemType; ActiveTargetName = rebound.TargetName;
        RewardItemType = rebound.RewardItemType; RewardItemCount = rebound.RewardItemCount;
        Progress = Math.Min(Progress, ActiveGoal);
        return changed;

	}
}
