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
	internal static bool RegisterMilestone(string providerMod, string milestoneKey, string displayName, Func<bool> completionProbe)
	{
		if (string.IsNullOrWhiteSpace(providerMod) || string.IsNullOrWhiteSpace(milestoneKey) || string.IsNullOrWhiteSpace(displayName) || completionProbe == null)
		{
			return false;
		}

		string key = $"milestone:{providerMod}/{milestoneKey}";
		if (!ExternalMilestones.TryAdd(key, completionProbe))
			return false;
		ExternalMilestoneNames[key] = displayName.Trim();
		initialized = false;
		return true;
	}

	private static int BossReward(NPC npc)
	{
		double healthThreat = Math.Sqrt(Math.Max(1, npc.lifeMax)) * 700d;
		double contactThreat = Math.Max(0, npc.damage) * 250d;
		int reward = (int)Math.Round((30_000d + healthThreat + contactThreat) / 100d) * 100;
		return Math.Clamp(reward, 50_000, 750_000);
	}

	private static ulong ComputeCatalogFingerprint()
	{
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offset;
		foreach (UniversalQuestTemplate template in Templates)
		{
			Mix(((byte)template.Kind).ToString());
			Mix(template.TargetKey);
			Mix(template.ItemType.ToString());
			Mix(template.Goal.ToString());
			Mix(template.RadiusTiles.ToString());
			Mix(template.BaseRewardCopper.ToString());
		}

		Mix(System.Text.Encoding.UTF8.GetString(DingDongBiomeHomes.Instance.GetFileBytes("Quests/ResidentStories.json")));
		return hash == 0 ? offset : hash;
		void Mix(string value)
		{
			foreach (char character in value ?? string.Empty)
			{
				hash ^= character;
				hash *= prime;
			}

			hash ^= 0xFF;
			hash *= prime;
		}
	}
}
