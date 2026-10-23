namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerRoom
{
	internal int Id { get; }
	internal WorldCompilerStructureKind Kind { get; }
	internal WorldCompilerRect Bounds { get; }
	internal int CenterX => Bounds.X + Bounds.Width / 2;
	internal int CenterY => Bounds.Y + Bounds.Height / 2;

	internal WorldCompilerRoom(int id, WorldCompilerStructureKind kind,
		WorldCompilerRect bounds)
	{
		Id = id;
		Kind = kind;
		Bounds = bounds;
	}
}
