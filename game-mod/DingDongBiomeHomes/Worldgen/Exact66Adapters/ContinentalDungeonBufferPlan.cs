using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly record struct ContinentalDungeonBufferPlan(
	int RoomCapacity, int JunctionCapacity)
{
	private const int VanillaRooms = 100;
	private const int VanillaJunctions = 500;

	internal static ContinentalDungeonBufferPlan Create(long tileCount,
		long vanillaTileCount)
	{
		long areaScale = Math.Max(1L,
			checked((tileCount + vanillaTileCount - 1L) / vanillaTileCount));
		long linearScale = Math.Max(1L, (long)Math.Ceiling(Math.Sqrt(areaScale)));
		int rooms = ContinentalScaleProfile.Capacity(VanillaRooms, linearScale);
		int junctions = ContinentalScaleProfile.Capacity(VanillaJunctions, areaScale);
		return new(rooms, junctions);
	}
}
