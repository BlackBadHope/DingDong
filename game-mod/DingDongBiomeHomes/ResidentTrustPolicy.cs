using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidentTrustPolicy
{
	internal static int RequiredQuests(NpcTownIdentity identity)
	{
		int baseline = Math.Clamp(ModContent.GetInstance<DingDongServerConfig>().RequiredResidentQuests, 1, 20);
		return RequiredQuests(baseline, identity.Biome, Main.hardMode);
	}

	internal static int RequiredQuests(int baseline, HomeBiome biome, bool hardmode)
	{
		baseline = Math.Clamp(baseline, 1, 20);
		int progressionTier = biome switch
		{
			HomeBiome.Forest or HomeBiome.Ocean => 0,
			HomeBiome.Snow or HomeBiome.Desert => 1,
			HomeBiome.Jungle or HomeBiome.Underground => 2,
			HomeBiome.Mushroom or HomeBiome.Hallow => 3,
			HomeBiome.Underworld => 4,
			_ => 0
		};
		int hardmodeMultiplier = hardmode ? 2 : 1;
		return Math.Clamp((baseline + progressionTier) * hardmodeMultiplier, 1, 40);
	}

	internal static bool ContractPass() =>
		RequiredQuests(3, HomeBiome.Forest, false) == 3 &&
		RequiredQuests(3, HomeBiome.Underworld, false) == 7 &&
		RequiredQuests(3, HomeBiome.Forest, true) == 6 &&
		RequiredQuests(3, HomeBiome.Underworld, true) == 14;

	internal static int Percent(ResidentQuestProgress progress, NpcTownIdentity identity)
	{
		int required = RequiredQuests(identity);
		return Percent(progress.Completed, required);
	}
	internal static int Percent(int completed, int required) =>
		(int)Math.Clamp((long)Math.Max(0, completed) * 100 / Math.Max(1, required), 0, 100);
}
