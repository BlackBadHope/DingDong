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
	internal static bool TryRebind(ResidentQuestDefinition saved, out ResidentQuestDefinition rebound)
	{
		if (saved.StoryKey.Length > 0) return PersonalResidentQuestCatalog.TryResolve(saved.StoryKey, out rebound);
		EnsureInitialized();
		rebound = saved;
		switch (saved.Kind)
		{
			case ResidentQuestKind.BossHunt:
				if (!NpcTargets.TryGetValue(saved.TargetKey, out NPC boss) || !boss.boss)
					return false;
				rebound = saved with
				{
					TargetName = boss.FullName
				};
				return true;
			case ResidentQuestKind.BannerCollection:
				if (!NpcTargets.TryGetValue(saved.TargetKey, out NPC bannerNpc))
					return false;
				int bannerId = Item.NPCtoBanner(bannerNpc.BannerID());
				int bannerItem = bannerId > 0 ? Item.BannerToItem(bannerId) : ItemID.None;
				if (bannerItem <= ItemID.None)
					return false;
				rebound = saved with
				{
					ItemType = bannerItem,
					TargetName = bannerNpc.FullName
				};
				return true;
			case ResidentQuestKind.BiomePurge:
				if (!saved.TargetKey.StartsWith(ModBiomePrefix, StringComparison.Ordinal))
					return Enum.TryParse(saved.TargetKey, ignoreCase: false, out HomeBiome _);
				if (!ModBiomes.TryGetValue(saved.TargetKey, out ModBiome biome))
					return false;
				rebound = saved with
				{
					TargetName = biome.DisplayName.Value
				};
				return true;
			case ResidentQuestKind.ModProgression:
				if (saved.TargetKey.StartsWith("milestone:", StringComparison.Ordinal))
				{
					if (!ExternalMilestones.ContainsKey(saved.TargetKey))
						return false;
					rebound = saved with
					{
						TargetName = ExternalMilestoneNames.TryGetValue(saved.TargetKey, out string milestoneName) ? milestoneName : saved.TargetName
					};
					return true;
				}

				if (!ModLoader.TryGetMod(saved.TargetKey, out Mod mod) || !HasUndefeatedBoss(saved.TargetKey))
					return false;
				rebound = saved with
				{
					TargetName = mod.DisplayName
				};
				return true;
			default:
				return true;
		}
	}

	internal static bool HasUndefeatedBoss(string modName)
	{
		EnsureInitialized();
		return BossTargetsByMod.TryGetValue(modName, out HashSet<string> targets) && targets.Any(target => !QuestWorldProgressSystem.WasBossDefeated(target));
	}

	internal static bool TryGetNpcTargetType(string targetKey, out int npcType)
	{
		EnsureInitialized();
		if (NpcTargets.TryGetValue(targetKey, out NPC npc))
		{
			npcType = npc.type;
			return npcType > NPCID.None;
		}

		npcType = NPCID.None;
		return false;
	}

	internal static bool TryGetNextUndefeatedBossType(string modName, out int npcType)
	{
		EnsureInitialized();
		if (BossTargetsByMod.TryGetValue(modName, out HashSet<string> targets))
		{
			foreach (string target in targets.OrderBy(value => value, StringComparer.Ordinal))
			{
				if (!QuestWorldProgressSystem.WasBossDefeated(target) && NpcTargets.TryGetValue(target, out NPC boss))
				{
					npcType = boss.type;
					return npcType > NPCID.None;
				}
			}
		}

		npcType = NPCID.None;
		return false;
	}

	internal static bool IsExternalMilestoneComplete(string targetKey) => ExternalMilestones.TryGetValue(targetKey, out Func<bool> probe) && SafeProbe(probe);
}
