using System;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalMemoryBudget(
	long TileBytes,
	long MapBytes,
	long ReserveBytes)
{
	private const long MiB = 1024L * 1024L;
	private const long GiB = 1024L * MiB;
	// Verified against tModLoader 1.4.4.9 (29bf978): the five SoA tile
	// planes are 2 + 2 + 2 + 1 + 8 bytes and MapTile is four bytes.
	internal const int TileBytesPerCell = 15;
	internal const int MapBytesPerCell = 4;
	internal const long CapacityReserveBytes = 2L * GiB;
	internal const long DeferredMapReserveBytes = 512L * MiB;

	internal long RequiredHeadroom =>
		checked(TileBytes + MapBytes + ReserveBytes);

	// The complete allocation must fit in the process commit limit, but it does
	// not need to be resident before allocation. Requiring TileBytes + MapBytes
	// as currently free physical RAM rejects healthy machines with a page file
	// after the mod pack has already consumed part of RAM. Keep the explicit
	// reserve resident so allocation cannot drive the machine into thrashing.
	internal long RequiredPhysicalSafety =>
		Math.Min(RequiredHeadroom, ReserveBytes);

	internal static ContinentalMemoryBudget ForCapacity(
		int width, int height, bool includeClientMap)
	{
		long cells = checked((long)width * height);
		return new(
			checked(cells * TileBytesPerCell),
			includeClientMap ? checked(cells * MapBytesPerCell) : 0L,
			CapacityReserveBytes);
	}

	internal static ContinentalMemoryBudget ForDeferredMap(int width, int height)
	{
		long cells = checked((long)width * height);
		return new(0L, checked(cells * MapBytesPerCell),
			DeferredMapReserveBytes);
	}
}
