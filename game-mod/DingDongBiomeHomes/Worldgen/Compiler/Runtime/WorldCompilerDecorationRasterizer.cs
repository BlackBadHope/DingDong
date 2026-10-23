using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerDecorationRasterizer
{
	internal static WorldCompilerDecorationReceipt Generate(WorldCompilerPlan plan)
	{
		var clock = Stopwatch.StartNew();
		long naturalTiles = 0;
		Parallel.ForEach(plan.Decorations.Entries,
			WorldCompilerExecutionPolicy.ParallelOptions, entry =>
			{
				if (IsReward(entry.Kind)) return;
				Interlocked.Add(ref naturalTiles, RasterNatural(plan, entry));
			});
		(int chests, int crystals) = PlaceRewards(plan);
		return new WorldCompilerDecorationReceipt(naturalTiles, chests, crystals,
			clock.ElapsedMilliseconds);
	}

	private static bool IsReward(WorldCompilerDecorationKind kind) => kind is
		WorldCompilerDecorationKind.SurfaceChest or
		WorldCompilerDecorationKind.UndergroundChest or
		WorldCompilerDecorationKind.DeepChest or
		WorldCompilerDecorationKind.LifeCrystal;
}
