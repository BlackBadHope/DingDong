namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerStructureReceipt
{
	internal long ChangedTiles { get; }
	internal int Rooms { get; }
	internal int Corridors { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerStructureReceipt(long changedTiles, int rooms,
		int corridors, long elapsedMilliseconds)
	{
		ChangedTiles = changedTiles;
		Rooms = rooms;
		Corridors = corridors;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
