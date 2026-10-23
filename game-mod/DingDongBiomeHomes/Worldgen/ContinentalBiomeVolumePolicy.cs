using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalBiomeVolumePolicy
{
	private const int SaharaAtContinentalHeight = 360;
	private const int ArcticAtContinentalHeight = 420;
	private const int AmazonAtContinentalHeight = 480;

	internal static int DesertDepth => Scale(SaharaAtContinentalHeight, 120, 480);
	internal static int ArcticDepth => Scale(ArcticAtContinentalHeight, 140, 560);
	internal static int AmazonDepth => Scale(AmazonAtContinentalHeight, 160, 640);

	internal static int Depth(ContinentalSurfaceKind kind) => kind switch
	{
		ContinentalSurfaceKind.Desert => DesertDepth,
		ContinentalSurfaceKind.Snow => ArcticDepth,
		_ => AmazonDepth
	};

	private static int Scale(int atContinentalHeight, int minimum, int maximum)
	{
		double heightRatio = Main.maxTilesY /
			(double)ContinentalWorldDimensions.Height;
		return Math.Clamp((int)Math.Round(atContinentalHeight * heightRatio),
			minimum, maximum);
	}
}
