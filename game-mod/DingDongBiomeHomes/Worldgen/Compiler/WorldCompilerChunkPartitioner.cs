using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerChunkPartitioner
{
	internal static WorldCompilerChunk[] Create(int width, int chunkWidth = 256,
		int halo = 16)
	{
		if (width <= 0 || chunkWidth <= 0 || halo < 0)
			throw new ArgumentOutOfRangeException(nameof(width));
		int count = (width + chunkWidth - 1) / chunkWidth;
		var chunks = new WorldCompilerChunk[count];
		for (int index = 0; index < count; index++)
		{
			int left = index * chunkWidth;
			int right = Math.Min(width, left + chunkWidth);
			chunks[index] = new WorldCompilerChunk(index, left, right,
				Math.Max(0, left - halo), Math.Min(width, right + halo));
		}
		return chunks;
	}
}
