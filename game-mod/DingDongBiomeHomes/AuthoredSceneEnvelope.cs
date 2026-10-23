using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

// Owned arrays, independent of Terraria's indexed Tile handles. Distance envelopes
// retain an authored cliff/tree's nearby supports without copying its rectangular
// source-world terrain crop. Every construction component is kept, not just one room.
internal static class AuthoredSceneEnvelope
{
	internal const int SupportRadius = 20;
	internal const int ClearanceRadius = 8;
	internal static int[] Distances(int width, int height, bool[] construction)
	{
		if (width <= 0 || height <= 0 || construction.Length != checked(width * height))
			throw new ArgumentException("Invalid authored scene dimensions.");
		int[] distance = new int[construction.Length];
		Array.Fill(distance, int.MaxValue);
		Queue<int> queue = new();
		for (int i = 0; i < construction.Length; i++)
			if (construction[i]) { distance[i] = 0; queue.Enqueue(i); }
		while (queue.Count > 0)
		{
			int index = queue.Dequeue(), next = distance[index] + 1;
			if (next > SupportRadius) continue;
			int x = index / height, y = index % height;
			if (x > 0) Visit(index - height);
			if (x + 1 < width) Visit(index + height);
			if (y > 0) Visit(index - 1);
			if (y + 1 < height) Visit(index + 1);
			void Visit(int target)
			{
				if (distance[target] <= next) return;
				distance[target] = next; queue.Enqueue(target);
			}
		}
		return distance;
	}
}
