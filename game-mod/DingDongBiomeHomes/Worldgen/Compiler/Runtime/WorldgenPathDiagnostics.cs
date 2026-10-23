using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldgenPathDiagnostics
{
	// Same three-by-three clearance as the acceptance contract, without early exit.
	internal static List<WorldCompilerPoint> Blocked(WorldCompilerWalkPath path, Func<int, int, bool> passable)
	{
		var blocked = new List<WorldCompilerPoint>();
		var seen = new HashSet<WorldCompilerPoint>();
		int elbowX = path.VerticalFirst ? path.FromX : path.ToX;
		int elbowY = path.VerticalFirst ? path.ToY : path.FromY;
		Segment(path.FromX, path.FromY, elbowX, elbowY);
		Segment(elbowX, elbowY, path.ToX, path.ToY);
		return blocked;
		void Segment(int ax, int ay, int bx, int by)
		{
			int steps = Math.Abs(ax - bx) + Math.Abs(ay - by);
			int dx = Math.Sign(bx - ax), dy = Math.Sign(by - ay);
			for (int step = 0; step <= steps; step++)
			for (int x = ax + dx * step - 1; x <= ax + dx * step + 1; x++)
			for (int y = ay + dy * step - 1; y <= ay + dy * step + 1; y++)
				if (!passable(x, y))
				{
					var point = new WorldCompilerPoint(x, y);
					if (seen.Add(point)) blocked.Add(point);
				}
		}
	}
}
