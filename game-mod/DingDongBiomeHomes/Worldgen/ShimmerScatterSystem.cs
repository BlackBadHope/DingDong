using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.OceanExpeditions;

namespace DingDongBiomeHomes.Worldgen;

// Owner, 29.10: one Shimmer lake per world is not enough. Right after vanilla places its own,
// 2-12 more are scattered through the deep layers of any biome: at least 700 tiles from each
// other and from the vanilla one, spread over the whole depth so they never line up in a row.
// Kept out of both oceans, the spawn column and the Underworld. Runs early, so later structure
// passes treat them like the vanilla lake; its own RNG leaves the shared world stream untouched.
public sealed class ShimmerScatterSystem : ModSystem
{
	private const double PassWeight = 40;
	private const int MinSpacing = 700;

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		if (Compiler.Runtime.WorldCompilerActivation.IsActive) return;
		int shimmer = tasks.FindIndex(t => t.Name == "Shimmer");
		if (shimmer < 0) return;
		tasks.Insert(shimmer + 1, new PassLegacy("DingDong scattered Shimmer", Scatter, PassWeight));
		totalWeight += PassWeight;
	}

	private void Scatter(GenerationProgress progress, GameConfiguration configuration)
	{
		int seed = Main.ActiveWorldFileData?.Seed ?? 0;
		var random = new Random(seed ^ 0x5141E7);
		int wanted = random.Next(2, 13), placed = 0, attempts = 0;
		var taken = new List<Vector2>();
		if (GenVars.shimmerPosition.X > 0) taken.Add(new((float)GenVars.shimmerPosition.X, (float)GenVars.shimmerPosition.Y));
		int ocean = OceanExpeditionLayout.WidthFor(Main.maxTilesX) + 250;
		int left = Math.Max(ocean, 300), right = Main.maxTilesX - Math.Max(ocean, 300);
		int top = (int)Main.rockLayer + 120, bottom = Main.UnderworldLayer - 220;
		if (right - left < 1000 || bottom - top < 300) return;
		IsolatedGenRand.Run(seed ^ 0x51A7E2, () =>
		{
			while (placed < wanted && attempts++ < 400)
			{
				progress.Set(placed / (double)wanted);
				int x = random.Next(left, right), y = random.Next(top, bottom);
				if (Math.Abs(x - Main.spawnTileX) < 350) continue;
				var at = new Vector2(x, y);
				bool clear = true;
				foreach (Vector2 other in taken)
					if (Vector2.Distance(other, at) < MinSpacing) { clear = false; break; }
				if (!clear || !WorldGen.ShimmerMakeBiome(x, y)) continue;
				taken.Add(at);
				placed++;
			}
		});
		Mod.Logger.Info($"DINGDONG_SHIMMER_SCATTER|wanted={wanted}|placed={placed}|attempts={attempts}|spacing={MinSpacing}|vanilla={GenVars.shimmerPosition.X:0},{GenVars.shimmerPosition.Y:0}");
	}
}
