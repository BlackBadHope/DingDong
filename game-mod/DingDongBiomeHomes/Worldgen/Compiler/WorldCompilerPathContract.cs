using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly record struct WorldCompilerWalkPath(int FromX, int FromY,
	int ToX, int ToY, bool VerticalFirst = false);

internal static class WorldCompilerPathContract
{
	internal static bool TryValidate(WorldCompilerWalkPath path,
		Func<int, int, bool> passable, out WorldCompilerPoint blocked)
	{
		int elbowX = path.VerticalFirst ? path.FromX : path.ToX;
		int elbowY = path.VerticalFirst ? path.ToY : path.FromY;
		return Segment(path.FromX, path.FromY, elbowX, elbowY, passable, out blocked) &&
			Segment(elbowX, elbowY, path.ToX, path.ToY, passable, out blocked);
	}

	private static bool Segment(int ax, int ay, int bx, int by,
		Func<int, int, bool> passable, out WorldCompilerPoint blocked)
	{
		int steps = Math.Abs(ax - bx) + Math.Abs(ay - by);
		int dx = Math.Sign(bx - ax), dy = Math.Sign(by - ay);
		for (int step = 0; step <= steps; step++)
		for (int x = ax + dx * step - 1; x <= ax + dx * step + 1; x++)
		for (int y = ay + dy * step - 1; y <= ay + dy * step + 1; y++)
			if (!passable(x, y)) { blocked = new(x, y); return false; }
		blocked = default;
		return true;
	}

	internal static bool Connected(int root, IReadOnlyList<int> nodes,
		IReadOnlyList<(int From, int To)> edges)
	{
		var reached = new HashSet<int> { root };
		for (int round = 0; round < nodes.Count; round++)
		{
			int before = reached.Count;
			foreach ((int from, int to) in edges)
			{
				if (reached.Contains(from)) reached.Add(to);
				if (reached.Contains(to)) reached.Add(from);
			}
			if (before == reached.Count) break;
		}
		foreach (int node in nodes) if (!reached.Contains(node)) return false;
		return true;
	}
}
