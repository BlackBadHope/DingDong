namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly record struct WorldCompilerReceiptSnapshot(long SolidTiles,
	long CarvedTiles, long OreTiles, long LiquidTiles, int Rooms,
	int ExpectedRooms, int Chests, int ExpectedChests, bool Finalized);

internal static class WorldCompilerReceiptContract
{
	internal static bool TryValidate(WorldCompilerReceiptSnapshot value,
		out string failure)
	{
		if (value.SolidTiles <= 0) return Fail("terrain-solid-empty", out failure);
		if (value.CarvedTiles <= 0) return Fail("caves-empty", out failure);
		if (value.OreTiles <= 0) return Fail("resources-empty", out failure);
		if (value.LiquidTiles <= 0) return Fail("hydrology-empty", out failure);
		if (value.Rooms != value.ExpectedRooms)
			return Fail($"structures={value.Rooms}/{value.ExpectedRooms}", out failure);
		if (value.Chests != value.ExpectedChests)
			return Fail($"chests={value.Chests}/{value.ExpectedChests}", out failure);
		if (!value.Finalized) return Fail("finalizer-missing", out failure);
		failure = string.Empty;
		return true;
	}

	private static bool Fail(string reason, out string failure)
	{
		failure = reason;
		return false;
	}
}
