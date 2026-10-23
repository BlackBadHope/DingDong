namespace DingDongBiomeHomes.Worldgen.Compiler;

internal enum WorldCompilerInfectionKind { Corruption, Crimson, Hallow }

internal readonly struct WorldCompilerInfection
{
	internal WorldCompilerInfectionKind Kind { get; }
	internal int SurfaceX { get; }
	internal int HalfWidth { get; }
	internal int SlopePermille { get; }

	internal WorldCompilerInfection(WorldCompilerInfectionKind kind, int surfaceX,
		int halfWidth, int slopePermille)
	{
		Kind = kind;
		SurfaceX = surfaceX;
		HalfWidth = halfWidth;
		SlopePermille = slopePermille;
	}
}
