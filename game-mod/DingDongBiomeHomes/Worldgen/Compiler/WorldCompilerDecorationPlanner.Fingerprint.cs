using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerDecorationPlanner
{
	private static ulong Fingerprint(List<WorldCompilerDecoration> entries)
	{
		ulong hash = 0x4445434F52415445UL;
		foreach (WorldCompilerDecoration entry in entries)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)entry.X << 32 ^
				(uint)entry.Y ^ (ulong)entry.Kind);
		return hash;
	}
}
