namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerPlanView
{
	internal int Width { get; }
	internal int Height { get; }
	internal ulong Seed { get; }
	internal ulong Fingerprint { get; }
	internal WorldCompilerBand[] Bands { get; }
	internal WorldCompilerSurfaceProfile Surface { get; }
	internal WorldCompilerChunk[] Chunks { get; }
	internal int CaveNodes { get; }
	internal int StructureRooms { get; }

	private WorldCompilerPlanView(WorldCompilerPlan plan)
	{
		Width = plan.Width;
		Height = plan.Height;
		Seed = plan.Seed;
		Fingerprint = plan.Fingerprint;
		Bands = plan.Bands;
		Surface = plan.Surface;
		Chunks = plan.Chunks;
		CaveNodes = plan.Caves.Nodes.Length;
		StructureRooms = plan.Structures.Rooms.Length;
	}

	internal static WorldCompilerPlanView From(WorldCompilerPlan plan) =>
		new(plan);
}
