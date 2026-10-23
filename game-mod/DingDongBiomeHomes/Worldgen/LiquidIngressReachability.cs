using System;
using System.Buffers;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Conservative local reachability, not a liquid-volume simulation. A source can
// fill connected space no higher than its original head. Solid walls block it;
// different liquid types are deliberately not treated as permanent barriers.
internal static class LiquidIngressReachability
{
	internal const byte Open = 1, Room = 2, Lava = 4;

	internal static bool CanReachRoom(byte[] cells, int width, int height, out int reached)
	{
		int count = checked(width * height);
		int[] heads = ArrayPool<int>.Shared.Rent(count);
		try
		{
			Array.Fill(heads, int.MaxValue, 0, count);
			var queue = new PriorityQueue<int, int>();
			for (int i = 0; i < count; i++)
				if ((cells[i] & (Open | Lava)) == (Open | Lava))
				{
					int y = i % height;
					heads[i] = y;
					queue.Enqueue(i, y);
				}
			while (queue.TryDequeue(out int i, out int head))
			{
				if (heads[i] != head) continue;
				if ((cells[i] & Room) != 0) { reached = i; return true; }
				int x = i / height, y = i % height;
				void Visit(int next, int nextY)
				{
					if (nextY < head || heads[next] <= head || (cells[next] & Open) == 0) return;
					heads[next] = head;
					queue.Enqueue(next, head);
				}
				if (x > 0) Visit(i - height, y);
				if (x + 1 < width) Visit(i + height, y);
				if (y > 0) Visit(i - 1, y - 1);
				if (y + 1 < height) Visit(i + 1, y + 1);
			}
			reached = -1;
			return false;
		}
		finally { ArrayPool<int>.Shared.Return(heads); }
	}
}
