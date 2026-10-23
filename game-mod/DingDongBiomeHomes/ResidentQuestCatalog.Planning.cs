using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	internal static ResidentQuestDefinition Resolve(NpcTownIdentity identity, int questId)
	{
		if (questId < 0 || questId >= PoolSize)
			throw new ArgumentOutOfRangeException(nameof(questId));
		if (questId >= BasePoolSize)
			return QuestContentRegistry.Resolve(identity, questId - BasePoolSize, questId);
		if (questId < SurveyCount)
			return new ResidentQuestDefinition(questId, ResidentQuestKind.SurveyOrigin, questId, ItemID.None, 1, 7, BaseRewardCopper: 2500);
		int local = questId - SurveyCount;
		if (local < PatrolCount)
			return new ResidentQuestDefinition(questId, ResidentQuestKind.PatrolOrigin, local, ItemID.None, 3 + local % 3, 6, BaseRewardCopper: 4000);
		local -= PatrolCount;
		if (local < SupplyCount)
		{
			(int itemType, int count) = ResolveSupply(identity, local);
			return new ResidentQuestDefinition(questId, ResidentQuestKind.DeliverSupplies, local, itemType, count, 0, BaseRewardCopper: 5000);
		}

		local -= SupplyCount;
		return new ResidentQuestDefinition(questId, ResidentQuestKind.DefendOrigin, local, ItemID.None, 6 + local, 64 + local % 4 * 24, BaseRewardCopper: 6000);
	}

	private static (int ItemType, int Count) ResolveSupply(NpcTownIdentity identity, int variant)
	{
		ResidentQuestContract homeContract = ResidentQuestContract.For(identity);
		const int baseSupplyVariants = 5;
		if (variant < baseSupplyVariants)
		{
			int baseStep = Math.Max(1, (homeContract.SupplyCount + 4) / 5);
			int count = Math.Max(1, homeContract.SupplyCount + (variant - 2) * baseStep);
			return (homeContract.SupplyItemType, count);
		}

		variant -= baseSupplyVariants;
		if (variant < CommonSupplies.Length)
			return CommonSupplies[variant];
		variant -= CommonSupplies.Length;
		(int itemType, int baseCount, int step) = ArchetypeSupply(identity.Archetype);
		return (itemType, baseCount + variant * step);
	}
}
