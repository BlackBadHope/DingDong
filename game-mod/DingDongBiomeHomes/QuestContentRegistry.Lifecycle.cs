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
	internal static void Reset()
	{
		initialized = false;
		catalogFingerprint = 0;
		Templates.Clear();
		ModBiomes.Clear();
		NpcTargets.Clear();
		BossTargetsByMod.Clear();
		ExternalMilestones.Clear();
		ExternalMilestoneNames.Clear();
	}
}
