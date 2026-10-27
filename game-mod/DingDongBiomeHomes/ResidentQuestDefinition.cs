using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal readonly record struct ResidentQuestDefinition(int Id, ResidentQuestKind Kind, int Variant, int ItemType, int Goal, int RadiusTiles, string TargetKey = "", string TargetName = "", int BaseRewardCopper = 0, string StoryKey = "", int RewardItemType = 0, int RewardItemCount = 0)
{
	internal bool IsValid => (StoryKey.Length > 0 ? Id >= PersonalResidentQuestCatalog.FirstId && Id < PersonalResidentQuestCatalog.FirstId + 12 : Id >= 0 && Id < ResidentQuestCatalog.PoolSize) && Goal > 0 && (Kind is not (ResidentQuestKind.DeliverSupplies or ResidentQuestKind.BannerCollection or ResidentQuestKind.Expedition) || ItemType > ItemID.None) && (Kind is not (ResidentQuestKind.BossHunt or ResidentQuestKind.BiomePurge or ResidentQuestKind.ModProgression) || !string.IsNullOrWhiteSpace(TargetKey));
}
