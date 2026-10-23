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
	private static void EnsureInitialized()
	{
		if (initialized)
			return;
		initialized = true;
		Templates.Clear();
		ModBiomes.Clear();
		NpcTargets.Clear();
		BossTargetsByMod.Clear();
		foreach (HomeBiome biome in Enum.GetValues<HomeBiome>())
		{
			Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.BiomePurge, biome.ToString(), biome.ToString(), ItemID.None, 18, 0, 12500));
		}

		foreach (ModBiome biome in ModContent.GetContent<ModBiome>().OrderBy(value => value.FullName, StringComparer.Ordinal))
		{
			string key = ModBiomePrefix + biome.FullName;
			ModBiomes[key] = biome;
			Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.BiomePurge, key, biome.DisplayName.Value, ItemID.None, 22, 0, 17500));
		}

		List<NPC> hostile = ContentSamples.NpcsByNetId.Values.Where(NpcLootEligibility.IsEligible).OrderBy(ContentKey, StringComparer.Ordinal).ToList();
		HashSet<string> bossMods = new(StringComparer.Ordinal);
		HashSet<string> bossTargets = new(StringComparer.Ordinal);
		HashSet<string> bannerTargets = new(StringComparer.Ordinal);
		foreach (NPC npc in hostile)
		{
			string contentKey = ContentKey(npc);
			NpcTargets.TryAdd(contentKey, npc);
			if (npc.boss && bossTargets.Add(contentKey))
			{
				string contentMod = ContentMod(npc);
				bossMods.Add(contentMod);
				if (!BossTargetsByMod.TryGetValue(contentMod, out HashSet<string> modTargets))
				{
					modTargets = new HashSet<string>(StringComparer.Ordinal);
					BossTargetsByMod[contentMod] = modTargets;
				}

				modTargets.Add(contentKey);
				Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.BossHunt, contentKey, npc.FullName, ItemID.None, 1, 0, BossReward(npc)));
			}

			int bannerNpcType = npc.BannerID();
			int bannerId = Item.NPCtoBanner(bannerNpcType);
			int bannerItem = bannerId > 0 ? Item.BannerToItem(bannerId) : ItemID.None;
			if (bannerItem > ItemID.None && bannerTargets.Add(contentKey))
			{
				Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.BannerCollection, contentKey, npc.FullName, bannerItem, 1, 0, 16000));
			}
		}

		foreach (string modName in bossMods.OrderBy(value => value, StringComparer.Ordinal))
		{
			if (string.Equals(modName, "Terraria", StringComparison.Ordinal))
				continue;
			string displayName = ModLoader.TryGetMod(modName, out Mod mod) ? mod.DisplayName : modName;
			Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.ModProgression, modName, displayName, ItemID.None, 1, 0, 75000));
		}

		foreach ((string key, Func<bool> probe)in ExternalMilestones.OrderBy(pair => pair.Key, StringComparer.Ordinal))
		{
			if (SafeProbe(probe))
				continue;
			Templates.Add(new UniversalQuestTemplate(ResidentQuestKind.ModProgression, key, ExternalMilestoneNames.TryGetValue(key, out string displayName) ? displayName : key["milestone:".Length..], ItemID.None, 1, 0, 100000));
		}

		catalogFingerprint = ComputeCatalogFingerprint();
	}
}
