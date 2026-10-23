namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalDungeonEdge(int X, int Y)
{
	internal bool IsValid => X >= 0 && Y >= 0;
	internal static ContinentalDungeonEdge Missing => new(-1, -1);
}
