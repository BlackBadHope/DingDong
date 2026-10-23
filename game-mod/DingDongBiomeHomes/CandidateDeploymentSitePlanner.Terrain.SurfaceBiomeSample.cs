using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private readonly record struct SurfaceBiomeSample(
		int Forest,
		int Desert,
		int Snow,
		int Jungle,
		int Ocean,
		int Hallow,
		int Purity,
		int Evil,
		int Water)
	{
		internal int TargetFor(HomeBiome biome) => biome switch
		{
			HomeBiome.Forest => Forest,
			HomeBiome.Desert => Desert,
			HomeBiome.Snow => Snow,
			HomeBiome.Jungle => Jungle,
			HomeBiome.Ocean => Ocean,
			HomeBiome.Hallow => Hallow,
			_ => 0
		};
	}

	private static SurfaceBiomeSample SampleSurfaceBiome(int centerX, int centerY)
	{
		int forest = 0, desert = 0, snow = 0, jungle = 0, ocean = 0, hallow = 0;
		int purity = 0, evil = 0, water = 0;
		int left = Math.Max(20, centerX - 80);
		int right = Math.Min(Main.maxTilesX - 20, centerX + 81);
		int top = Math.Max(20, centerY - 45);
		int bottom = Math.Min(Main.maxTilesY - 20, centerY + 56);
		for (int x = left; x < right; x += 3)
		{
			for (int y = top; y < bottom; y += 3)
			{
				Tile tile = Main.tile[x, y];
				if (IsOceanWater(tile)) water++;
				if (!tile.HasTile) continue;
				ushort type = tile.TileType;
				if (MatchesBiomeTile(type, HomeBiome.Forest)) forest++;
				if (MatchesBiomeTile(type, HomeBiome.Desert)) desert++;
				if (MatchesBiomeTile(type, HomeBiome.Snow)) snow++;
				if (MatchesBiomeTile(type, HomeBiome.Jungle)) jungle++;
				if (MatchesBiomeTile(type, HomeBiome.Ocean)) ocean++;
				if (MatchesBiomeTile(type, HomeBiome.Hallow)) hallow++;
				if (type is TileID.Dirt or TileID.Grass or TileID.Stone or TileID.ClayBlock) purity++;
				if (IsEvil(type)) evil++;
			}
		}
		return new(forest, desert, snow, jungle, ocean, hallow, purity, evil, water);
	}
}
