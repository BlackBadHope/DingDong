using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal static partial class ResidentQuestCatalog
{
	internal const int SurveyCount = 8;
	internal const int PatrolCount = 12;
	internal const int SupplyCount = 16;
	internal const int DefenseCount = 12;
	internal const int BasePoolSize = SurveyCount + PatrolCount + SupplyCount + DefenseCount;
	internal static int PoolSize => BasePoolSize + QuestContentRegistry.TemplateCount;

	private static readonly (int ItemType, int Count)[] CommonSupplies =
	{
		(ItemID.Torch, 20),
		(ItemID.Rope, 50),
		(ItemID.Gel, 30),
		(ItemID.LesserHealingPotion, 5),
		(ItemID.RecallPotion, 3),
		(ItemID.Acorn, 10)
	};
	private static readonly ResidentQuestKind[] GuidedKindBag =
	{
		ResidentQuestKind.SurveyOrigin,
		ResidentQuestKind.PatrolOrigin,
		ResidentQuestKind.DeliverSupplies,
		ResidentQuestKind.BiomePurge,
		ResidentQuestKind.BannerCollection,
		ResidentQuestKind.BossHunt,
		ResidentQuestKind.DefendOrigin,
		ResidentQuestKind.PatrolOrigin,
		ResidentQuestKind.DeliverSupplies,
		ResidentQuestKind.BiomePurge,
		ResidentQuestKind.BannerCollection,
		ResidentQuestKind.ModProgression
	};
}
