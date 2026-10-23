using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static bool MatchesSurfaceBiome(int centerX, int centerY, HomeBiome biome, out string fallback, bool requireOceanWater = true, bool allowWeatheredOpenCoast = false)
	{
		SurfaceBiomeSample sample = SampleSurfaceBiome(centerX, centerY);
		return MatchesSurfaceBiomeSample(centerX, biome, sample, out fallback,
			requireOceanWater, allowWeatheredOpenCoast);
	}

	private static bool MatchesSurfaceBiomeSample(int centerX, HomeBiome biome,
		SurfaceBiomeSample sample, out string fallback, bool requireOceanWater = true,
		bool allowWeatheredOpenCoast = false)
	{
		int target = sample.TargetFor(biome);
		int purity = sample.Purity;
		int evil = sample.Evil;
		int water = sample.Water;

		fallback = "ExactBiomeOnly";
		if (biome == HomeBiome.Ocean)
		{
			bool coastal = centerX <= Main.maxTilesX / 6 || centerX >= Main.maxTilesX * 5 / 6;
			bool waterSatisfied = !requireOceanWater || water >= 12;
			bool evilSatisfied = evil <= 6 || allowWeatheredOpenCoast;
			string grade = allowWeatheredOpenCoast && evil > 6 ? "WeatheredNaturalOceanCoast" : requireOceanWater ? "NaturalOceanCoast" : "NaturalOceanCoastalGrade";
			fallback = centerX < Main.maxTilesX / 2 ? $"Left{grade}" : $"Right{grade}";
			return coastal && waterSatisfied && evilSatisfied;
		}

		if (biome == HomeBiome.Hallow)
		{
			if (target >= 24 && evil <= 6)
				return true;
			fallback = "PuritySurfaceForPreHardmodeHallow";
			return purity >= 40 && evil <= 6;
		}

		int required = biome switch
		{
			HomeBiome.Forest => 30,
			HomeBiome.Snow => 24,
			_ => 36
		};
		return target >= required && evil <= 6;
	}

	private static bool MatchesBiomeTile(ushort type, HomeBiome biome) => biome switch
	{
		HomeBiome.Forest => type is TileID.Grass or TileID.Dirt,
		HomeBiome.Desert or HomeBiome.Ocean => type is TileID.Sand or TileID.HardenedSand or TileID.Sandstone,
		HomeBiome.Snow => type is TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce,
		HomeBiome.Jungle => type is TileID.JungleGrass or TileID.Mud,
		HomeBiome.Mushroom => type == TileID.MushroomGrass,
		HomeBiome.Hallow => type is TileID.HallowedGrass or TileID.Pearlstone or TileID.Pearlsand or TileID.HallowHardenedSand or TileID.HallowSandstone or TileID.HallowedIce,
		HomeBiome.Underworld => type is TileID.Ash or TileID.Hellstone or TileID.Obsidian or TileID.ObsidianBrick or TileID.HellstoneBrick,
		_ => false
	};
	private static bool IsEvil(ushort type) => type is TileID.CorruptGrass or TileID.Ebonstone or TileID.Ebonsand or TileID.CorruptHardenedSand or TileID.CorruptSandstone or TileID.CorruptIce or TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimsand or TileID.CrimsonHardenedSand or TileID.CrimsonSandstone or TileID.FleshIce;
}
