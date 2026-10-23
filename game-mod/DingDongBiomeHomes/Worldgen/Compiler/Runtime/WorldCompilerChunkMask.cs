using System;
using System.Buffers;
using System.Numerics;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal interface IWorldCompilerChunkMaskVisitor
{
	void Visit(int x, int y);
}

internal sealed class WorldCompilerChunkMask : IDisposable
{
	private readonly int left;
	private readonly int width;
	private readonly int height;
	private readonly int bitCount;
	private readonly int wordCount;
	private ulong[] words;

	internal WorldCompilerChunkMask(WorldCompilerChunk chunk, int height)
	{
		if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
		left = chunk.Left;
		width = chunk.Right - chunk.Left;
		this.height = height;
		bitCount = checked(width * height);
		wordCount = (bitCount + 63) / 64;
		words = ArrayPool<ulong>.Shared.Rent(wordCount);
		Array.Clear(words, 0, wordCount);
	}

	internal void MarkEllipse(int centerX, int centerY, int radiusX, int radiusY)
	{
		int[] profile = WorldCompilerEllipseProfile.Get(radiusX, radiusY);
		int firstX = Math.Max(left, centerX - radiusX);
		int lastX = Math.Min(left + width - 1, centerX + radiusX);
		for (int x = firstX; x <= lastX; x++)
		{
			int extent = profile[x - centerX + radiusX];
			MarkVerticalRange(x, Math.Max(1, centerY - extent),
				Math.Min(height - 2, centerY + extent));
		}
	}

	internal void Apply<TVisitor>(ref TVisitor visitor)
		where TVisitor : struct, IWorldCompilerChunkMaskVisitor
	{
		for (int wordIndex = 0; wordIndex < wordCount; wordIndex++)
		{
			ulong pending = words[wordIndex];
			while (pending != 0)
			{
				int bit = BitOperations.TrailingZeroCount(pending);
				int flat = wordIndex * 64 + bit;
				if (flat >= bitCount) return;
				visitor.Visit(left + flat / height, flat % height);
				pending &= pending - 1;
			}
		}
	}

	private void MarkVerticalRange(int x, int top, int bottom)
	{
		if (top > bottom) return;
		int start = (x - left) * height + top;
		int end = (x - left) * height + bottom;
		int firstWord = start >> 6;
		int lastWord = end >> 6;
		int firstBit = start & 63;
		int lastBit = end & 63;
		if (firstWord == lastWord)
		{
			words[firstWord] |= (ulong.MaxValue << firstBit) &
				(ulong.MaxValue >> (63 - lastBit));
			return;
		}
		words[firstWord] |= ulong.MaxValue << firstBit;
		for (int index = firstWord + 1; index < lastWord; index++)
			words[index] = ulong.MaxValue;
		words[lastWord] |= ulong.MaxValue >> (63 - lastBit);
	}

	public void Dispose()
	{
		if (words == null) return;
		Array.Clear(words, 0, wordCount);
		ArrayPool<ulong>.Shared.Return(words);
		words = null;
	}
}
