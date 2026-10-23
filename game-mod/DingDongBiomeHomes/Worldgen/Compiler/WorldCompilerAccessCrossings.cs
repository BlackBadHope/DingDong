using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerAccessCrossings
{
	// An access shaft is rasterized after corridors. Reopen only the planned
	// dungeon crossings through that shaft; never introduce a bypass to a temple gate.
	internal static IEnumerable<WorldCompilerRect> Openings(WorldCompilerRect shaft,
		bool locked, IReadOnlyList<WorldCompilerRoom> rooms,
		IReadOnlyList<WorldCompilerCorridor> corridors)
	{
		if (locked) yield break;
		foreach (var corridor in corridors)
		{
			var from = rooms[corridor.From];
			var to = rooms[corridor.To];
			if (from.Kind != WorldCompilerStructureKind.DungeonRoom || to.Kind != from.Kind) continue;
			// Match the existing walking contract's three-by-three clearance.
			var horizontal = new WorldCompilerRect(Math.Min(from.CenterX, to.CenterX) - 1,
				from.CenterY - 1, Math.Abs(to.CenterX - from.CenterX) + 3, 3);
			var vertical = new WorldCompilerRect(to.CenterX - 1,
				Math.Min(from.CenterY, to.CenterY) - 1, 3, Math.Abs(to.CenterY - from.CenterY) + 3);
			foreach (var strip in new[] { horizontal, vertical })
			{
				int left = Math.Max(shaft.X, strip.X), top = Math.Max(shaft.Y, strip.Y);
				int right = Math.Min(shaft.Right, strip.Right), bottom = Math.Min(shaft.Bottom, strip.Bottom);
				if (left < right && top < bottom)
					yield return new(left, top, right - left, bottom - top);
			}
		}
	}
}
