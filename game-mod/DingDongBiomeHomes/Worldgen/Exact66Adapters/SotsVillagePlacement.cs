using System;
using DingDongBiomeHomes.Worldgen.Compiler;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly record struct SotsVillageSite(int X, int Y, int Ground,
	WorldCompilerRect Outer, WorldCompilerRect Sweep, WorldCompilerRect Access,
	WorldCompilerRect Entrance);

internal static class SotsVillagePlacement
{
	internal static SotsVillageSite Find(int desiredX, int desiredY, int width, int height,
		WorldCompilerRect evil, int worldWidth, int worldHeight,
		Func<WorldCompilerRect, bool> protectedArea, Func<WorldCompilerRect, bool> nativeValid,
		Func<int, int> findGround)
	{
		int noGround = 0, bounds = 0, sweep = 0, access = 0, entrance = 0, native = 0;
		// At most 33 depths x 65 columns; no native "accept invalid after 1000" fallback.
		for (int depth = 0; depth <= 32; depth++)
		for (int column = 0; column <= 64; column++)
		{
			int offset = column == 0 ? 0 : ((column + 1) / 2) * 32 * (column % 2 == 1 ? 1 : -1);
			int x = Math.Clamp(desiredX + offset, evil.X + 1, evil.Right - 1);
			int y = desiredY + depth * 48;
			int ground = findGround(x);
			if (ground < 100) { noGround++; continue; }
			SotsVillageSite site = At(x, y, width, height, ground);
			if (site.Sweep.X < 100 || site.Sweep.Right >= worldWidth - 100 ||
				site.Sweep.Y < 100 || site.Sweep.Bottom >= worldHeight - 320) { bounds++; continue; }
			if (protectedArea(site.Sweep)) { sweep++; continue; }
			if (protectedArea(site.Access)) { access++; continue; }
			if (protectedArea(site.Entrance)) { entrance++; continue; }
			if (nativeValid(site.Outer)) return site;
			native++;
		}
		throw new InvalidOperationException($"SOTS village has no admitted site: desired={desiredX},{desiredY}; size={width},{height}; evil={evil.X},{evil.Y},{evil.Width},{evil.Height}; " +
			$"rejections ground={noGround}, bounds={bounds}, sweep={sweep}, access={access}, entrance={entrance}, native={native} (2145 candidates maximum).");
	}

	internal static SotsVillageSite At(int x, int y, int width, int height, int ground)
	{
		WorldCompilerRect outer = new(x - width / 2 - 40, y - height / 2 - 40,
			width + 80, height + 80);
		// Native cleanup scans inclusive edges and its decoration reaches outside them.
		WorldCompilerRect sweep = new(outer.X - 70, outer.Y - 40,
			outer.Width + 140, outer.Height + 70);
		// Marker34 in the pinned native entrance template is at column36 (origin x-36):
		// descent begins at exactly x,ground+11. Zero steering jitter keeps x fixed.
		// Tunnel outline reaches +/-13; circles +/-12. Wider conversion/ambient writes
		// have separate per-cell/footprint reservation guards.
		int top = Math.Min(ground - 20, sweep.Y);
		WorldCompilerRect access = new(x - 20, top, 41, sweep.Bottom - top + 9);
		WorldCompilerRect entrance = new(x - 58, ground - 58, 117, 117);
		return new(x, y, ground, outer, sweep.Inflate(8), access, entrance);
	}
}
