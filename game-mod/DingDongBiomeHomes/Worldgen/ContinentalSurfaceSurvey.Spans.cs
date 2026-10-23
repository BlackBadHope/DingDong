using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal sealed partial class ContinentalSurfaceSurvey
{
	internal ContinentalSurfaceKind OwnerAt(int x)
	{
		if (ForestReserve.Contains(x) || x < Main.maxTilesX / 30 || x >= Main.maxTilesX - Main.maxTilesX / 30)
			return ContinentalSurfaceKind.Other;
		ContinentalSurfaceKind native = kind[x];
		if (native == ContinentalSurfaceKind.Evil)
			return ContinentalSurfaceKind.Other;
		ContinentalSurfaceKind best = ContinentalSurfaceKind.Other;
		int distance = int.MaxValue;
		Consider(DesertTarget, DesertSource, ContinentalSurfaceKind.Desert, native, x, ref best, ref distance);
		Consider(SnowTarget, SnowSource, ContinentalSurfaceKind.Snow, native, x, ref best, ref distance);
		Consider(JungleTarget, JungleSource, ContinentalSurfaceKind.Jungle, native, x, ref best, ref distance);
		return best;
	}

	private ContinentalSpan FindSpan(ContinentalSurfaceKind target)
	{
		int run = -1, last = -1, bestLeft = -1, bestRight = -2, hits = 0, bestHits = 0;
		for (int x = Main.maxTilesX / 30; x < Main.maxTilesX - Main.maxTilesX / 30; x++)
		{
			if (kind[x] == target)
			{
				if (run < 0) run = x;
				last = x;
				hits++;
			}
			else if (run >= 0 && x - last > MaximumSpanGap)
			{
				CommitSpan(run, last, hits, ref bestLeft, ref bestRight, ref bestHits);
				run = -1;
				hits = 0;
			}
		}
		if (run >= 0)
			CommitSpan(run, last, hits, ref bestLeft, ref bestRight, ref bestHits);
		return new ContinentalSpan(bestLeft, bestRight);
	}

	private ContinentalSpan TargetAround(ContinentalSpan source, double fraction)
	{
		int width = Math.Max(source.Width, (int)Math.Round(Main.maxTilesX * fraction));
		int left = source.Center - width / 2;
		if (left <= ForestReserve.Right && left + width - 1 >= ForestReserve.Left)
			left = source.Center < ForestCenterX ? ForestReserve.Left - 64 - width : ForestReserve.Right + 64;
		int margin = Main.maxTilesX / 30;
		left = Math.Clamp(left, margin, Main.maxTilesX - margin - width);
		return new ContinentalSpan(left, left + width - 1);
	}

	private ContinentalSpan ClampSpan(int left, int right) =>
		new(Math.Clamp(left, 40, Main.maxTilesX - 41), Math.Clamp(right, 40, Main.maxTilesX - 41));

	private static void Consider(ContinentalSpan target, ContinentalSpan source,
		ContinentalSurfaceKind candidate, ContinentalSurfaceKind native, int x,
		ref ContinentalSurfaceKind best, ref int distance)
	{
		bool nativeConflict = native is ContinentalSurfaceKind.Desert or ContinentalSurfaceKind.Snow or ContinentalSurfaceKind.Jungle;
		if (!target.Contains(x) || nativeConflict && native != candidate)
			return;
		int next = source.DistanceTo(x);
		if (next < distance)
		{
			best = candidate;
			distance = next;
		}
	}

	private static void CommitSpan(int left, int right, int hits,
		ref int bestLeft, ref int bestRight, ref int bestHits)
	{
		if (hits <= bestHits)
			return;
		bestLeft = left;
		bestRight = right;
		bestHits = hits;
	}
}
