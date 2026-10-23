using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalAmazonTrees
{
	internal const int MinTreeHeight = 120;
	internal const int MaxTreeHeight = 240;
	internal const int MaxTreeAttemptsPerTree = 24;
	private const int TreeHalfWidth = 76;
	private const int TreeSpacing = 200;

	internal static ContinentalAmazonTreeResult Place(ContinentalSurfaceSurvey survey)
	{
		ContinentalSpan jungle = survey.JungleTarget;
		int target = Math.Clamp(jungle.Width / 260, 14, 28);
		int attemptBudget = target * MaxTreeAttemptsPerTree;
		int attempts = 0;
		bool[] allowed = ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
		var plans = new List<(Rectangle Area, int X, int Ground, int Height)>(target);
		for (; attempts < attemptBudget && plans.Count < target; attempts++)
		{
			int x = WorldGen.genRand.Next(jungle.Left + 80, jungle.Right - 79);
			if (TooClose(plans, x)) continue;
			int ground = FindGround(x, survey.GroundAt(x));
			int height = WorldGen.genRand.Next(MinTreeHeight, MaxTreeHeight + 1);
			Rectangle area = new(x - TreeHalfWidth, ground - height - 30,
				TreeHalfWidth * 2 + 1, height + 62);
			if (ground < 0 || area.Top < 30 || area.Bottom >= Main.maxTilesY - 30 ||
				!GenVars.structures.CanPlace(area, allowed, 18))
				continue;
			plans.Add((area, x, ground, height));
		}
		if (plans.Count != target)
			throw new InvalidOperationException(
				$"Amazon landmark withheld: placed={plans.Count}; target={target}; attempts={attempts}/{attemptBudget}; mutated=0.");
		foreach (var plan in plans)
			GenVars.structures.AddProtectedStructure(plan.Area, 18);
		foreach (var plan in plans)
		{
			ContinentalAmazonTreeBuilder.Build(plan.Area, plan.X, plan.Ground, plan.Height);
		}
		return new(plans.Count, target, attempts, attemptBudget);
	}

	private static int FindGround(int x, int expected)
	{
		if (expected < 0) return -1;
		int top = Math.Max(30, expected - 96);
		int bottom = Math.Min(Main.maxTilesY - 30, expected + 160);
		for (int y = top; y < bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && Main.tileSolid[tile.TileType] &&
				!Main.tileSolidTop[tile.TileType])
				return y;
		}
		return -1;
	}

	private static bool TooClose(
		List<(Rectangle Area, int X, int Ground, int Height)> plans, int x)
	{
		foreach (var plan in plans)
			if (Math.Abs(plan.X - x) < TreeSpacing) return true;
		return false;
	}
}
