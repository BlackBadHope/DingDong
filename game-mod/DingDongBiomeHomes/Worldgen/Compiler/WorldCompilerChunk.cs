namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerChunk
{
	internal int Index { get; }
	internal int Left { get; }
	internal int Right { get; }
	internal int ReadLeft { get; }
	internal int ReadRight { get; }

	internal WorldCompilerChunk(int index, int left, int right, int readLeft,
		int readRight)
	{
		Index = index;
		Left = left;
		Right = right;
		ReadLeft = readLeft;
		ReadRight = readRight;
	}
}
