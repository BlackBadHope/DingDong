namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerStructurePlan
{
	internal WorldCompilerRoom[] Rooms { get; }
	internal WorldCompilerCorridor[] Corridors { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerStructurePlan(WorldCompilerRoom[] rooms,
		WorldCompilerCorridor[] corridors, ulong fingerprint)
	{
		Rooms = rooms;
		Corridors = corridors;
		Fingerprint = fingerprint;
	}
}
