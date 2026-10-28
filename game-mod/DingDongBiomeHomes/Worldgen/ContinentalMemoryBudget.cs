using System;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalMemoryBudget(
	long TileBytes,
	long MapBytes,
	long ReserveBytes,
	long PhysicalFloorBytes = 0)
{
	private const long MiB = 1024L * 1024L;
	private const long GiB = 1024L * MiB;
	// Verified against tModLoader 1.4.4.9 (29bf978): the five SoA tile
	// planes are 2 + 2 + 2 + 1 + 8 bytes and MapTile is four bytes.
	// RamOpt 28.10: mod planes (ITileData) are added at runtime, see RamOptTilePlanes.
	internal const int TileBytesPerCell = RamOptTilePlanes.VanillaBytesPerCell;
	internal const int MapBytesPerCell = 4;
	// RamOpt 28.10 (review fix): reserves derived from the S5 x10 server.log. Private grew
	// from 952 MiB (pass Reset, before any plane: planes register lazily) to 5192 MiB
	// (end of "DingDong authored biome settlements"); every 17 B/cell plane (3269 MiB:
	// Terrain +1958, Dunes +588, Dirt Wall Backgrounds +389, Granite +195, Brimstone
	// Crag +195) is inside that peak, so the measured non-plane growth is
	// 5192 - 952 - 3269 = 971 MiB. 5192 is the maximum over pass ENDS, not the true
	// in-pass peak, and S5 ran with System.GC.ConserveMemory=7 (stock players do not),
	// so both tiers keep an explicit margin on top of the measurement.
	// fast: 971 MiB + 565 MiB (~58 %) margin = 1.5 GiB.
	internal const long CapacityReserveBytes = 1536L * MiB;
	// Free RAM floor of the fast tier stays the previous 2 GiB.
	internal const long CapacityPhysicalFloorBytes = 2L * GiB;
	// economical (generation only): 971 - 103 MiB integrity snapshot streamed to disk
	// = 868 MiB, + 25 % margin = 1085, rounded up to 1088 MiB. Free RAM floor 1 GiB.
	// Presets 28.10 review fix: B9 (recompute to 1472 MiB = (1264 - 103) x 1.25, rounded up) is
	// DEFERRED. It would change x10 admission (lowMemoryCommitRequired 4568649105 -> 4971302289 B,
	// denying S6 states at 4.31-4.46 GiB), and the 1264 MiB input was measured in the fast tier with
	// stock GC, not in economical mode. Recompute only after S7Eco or a dedicated economical x10 run
	// measures non-plane growth under this tier; until then x10 keeps the measured 1088 MiB.
	internal const long EconomicalReserveBytes = 1088L * MiB;
	internal const long EconomicalPhysicalFloorBytes = 1L * GiB;
	internal const long DeferredMapReserveBytes = 512L * MiB;

	internal long RequiredHeadroom =>
		checked(TileBytes + MapBytes + ReserveBytes);

	// The complete allocation must fit in the process commit limit, but it does
	// not need to be resident before allocation. Requiring TileBytes + MapBytes
	// as currently free physical RAM rejects healthy machines with a page file
	// after the mod pack has already consumed part of RAM. Keep the explicit
	// reserve resident so allocation cannot drive the machine into thrashing.
	internal long RequiredPhysicalSafety =>
		Math.Min(RequiredHeadroom, PhysicalFloorBytes > 0 ? PhysicalFloorBytes : ReserveBytes);

	internal static ContinentalMemoryBudget ForCapacity(
		int width, int height, bool includeClientMap, bool lowMemory = false)
	{
		long cells = checked((long)width * height);
		// RamOpt 28.10: count every plane that will be allocated at this length. Planes
		// register lazily (first access), so "registered now" can be zero; count all
		// loaded ITileData structs (67-mod profile: 17 B per cell, measured).
		int bytesPerCell = RamOptTilePlanes.AllKnownBytesPerCell(out _);
		return new(
			checked(cells * Math.Max(TileBytesPerCell, bytesPerCell)),
			includeClientMap ? checked(cells * MapBytesPerCell) : 0L,
			lowMemory ? EconomicalReserveBytes : CapacityReserveBytes,
			lowMemory ? EconomicalPhysicalFloorBytes : CapacityPhysicalFloorBytes);
	}

	internal static ContinentalMemoryBudget ForDeferredMap(int width, int height)
	{
		long cells = checked((long)width * height);
		return new(0L, checked(cells * MapBytesPerCell),
			DeferredMapReserveBytes);
	}
}
