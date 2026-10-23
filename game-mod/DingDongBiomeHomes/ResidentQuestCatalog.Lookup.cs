using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	internal static int SelectQuestId(string persistentId, string npcKey, int completed)
	{
		int ordinal = Math.Max(0, completed);
		int cycle = ordinal / PoolSize;
		int position = ordinal % PoolSize;
		int[] order = BuildOrder(persistentId, npcKey, cycle);
		return order[position];
	}

	internal static ResidentQuestDefinition SelectAvailable(string persistentId, NpcTownIdentity identity, int completed, bool allowResidenceObjectives = true)
	{
		int ordinal = Math.Max(0, completed);
		int cycle = ordinal / Math.Max(1, PoolSize);
		ResidentQuestDefinition[] ordered = BuildOrder(persistentId, identity.NpcKey, cycle)
			.Select(questId => Resolve(identity, questId)).ToArray();
		ResidentQuestDefinition[] available = ordered
			.Where(definition => IsCurrentlyAvailable(definition) &&
				(allowResidenceObjectives || !RequiresResidence(definition.Kind)))
			.ToArray();
		if (available.Length == 0)
		{
			ResidentQuestDefinition[] safeFallback = allowResidenceObjectives
				? ordered
				: ordered.Where(definition => !RequiresResidence(definition.Kind)).ToArray();
			return safeFallback.Length > 0 ? safeFallback[0] : ordered[0];
		}
		ResidentQuestKind preferred = PreferredKind(persistentId, identity.NpcKey, ordinal);
		ResidentQuestDefinition[] preferredPool = available.Where(definition => definition.Kind == preferred).ToArray();
		if (preferredPool.Length == 0 && preferred is ResidentQuestKind.BossHunt or ResidentQuestKind.ModProgression)
		{
			preferredPool = available.Where(definition => definition.Kind is ResidentQuestKind.BossHunt or ResidentQuestKind.ModProgression).ToArray();
		}

		ResidentQuestDefinition[] pool = preferredPool.Length > 0 ? preferredPool : available;
		uint selection = StableHash($"quest-3.0-target|{persistentId}|{identity.NpcKey}|{ordinal}|{preferred}");
		return pool[(int)(selection % (uint)pool.Length)];
		static bool IsCurrentlyAvailable(ResidentQuestDefinition definition)
		{
			if (!QuestContentRegistry.TargetExists(definition))
				return false;
			if (definition.Kind == ResidentQuestKind.BossHunt && QuestWorldProgressSystem.WasBossDefeated(definition.TargetKey))
				return false;
			if (definition.Kind == ResidentQuestKind.ModProgression && definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) && QuestContentRegistry.IsExternalMilestoneComplete(definition.TargetKey))
				return false;
			return true;
		}

		static bool RequiresResidence(ResidentQuestKind kind) =>
			kind is ResidentQuestKind.SurveyOrigin
				or ResidentQuestKind.PatrolOrigin
				or ResidentQuestKind.DefendOrigin;
	}
}
