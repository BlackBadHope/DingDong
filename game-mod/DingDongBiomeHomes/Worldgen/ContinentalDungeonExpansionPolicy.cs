using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonExpansionPolicy
{
	internal const int MinimumPortals = 3;
	internal const int MaximumPortals = 5;
	internal const int MaximumRooms = 72;
	internal const int MaximumRoomsPerBranch = 15;

	internal static int TargetRooms(out double areaDeficit)
	{
		double vanillaArea = (double)ContinentalWorldDimensions.VanillaLargeWidth *
			ContinentalWorldDimensions.VanillaLargeHeight;
		double areaScale = Main.maxTilesX * (double)Main.maxTilesY / vanillaArea;
		double linearScale = Main.maxTilesX /
			(double)ContinentalWorldDimensions.VanillaLargeWidth;
		areaDeficit = Math.Max(0d, areaScale - linearScale);
		return Math.Clamp((int)Math.Round(6d + areaDeficit * 9d), 18,
			MaximumRooms);
	}

	internal static int PortalCount(int rooms) =>
		Math.Clamp((rooms + MaximumRoomsPerBranch - 1) / MaximumRoomsPerBranch,
			MinimumPortals, MaximumPortals);

	internal static int AttemptBudget(int rooms) => Math.Clamp(rooms * 10, 300, 720);
}
