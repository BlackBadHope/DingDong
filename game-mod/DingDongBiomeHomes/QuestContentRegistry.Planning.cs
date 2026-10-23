using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Builds quest targets from the content that is actually loaded. This keeps
/// boss, banner and biome quests compatible with content mods without taking a
/// hard assembly dependency on each of them.
/// </summary>
internal static partial class QuestContentRegistry
{
	internal static ResidentQuestDefinition Resolve(NpcTownIdentity identity, int dynamicIndex, int questId)
	{
		EnsureInitialized();
		if (dynamicIndex < 0 || dynamicIndex >= Templates.Count)
			throw new ArgumentOutOfRangeException(nameof(dynamicIndex));
		UniversalQuestTemplate template = Templates[dynamicIndex];
		return new ResidentQuestDefinition(questId, template.Kind, identity.Variant, template.ItemType, template.Goal, template.RadiusTiles, template.TargetKey, template.TargetName, template.BaseRewardCopper);
	}
}
