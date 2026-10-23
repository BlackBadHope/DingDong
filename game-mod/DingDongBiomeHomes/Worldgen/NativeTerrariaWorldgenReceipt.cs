using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeTerrariaWorldgenReceipt
{
	private static readonly string[] Required = ["Terrain", "Tunnels",
		"Dirt Layer Caves", "Rock Layer Caves", "Surface Caves",
		"Generate Ice Biome", "Jungle", "Full Desert", "Underworld",
		"Corruption", "Lakes", "Dungeon", "Hives", "Buried Chests",
		"Water Chests", "Living Trees", "Micro Biomes", "Final Cleanup"];
	private static string[] expected = [];
	private static readonly List<string> completed = [];
	internal static bool Complete { get; private set; }
	// Final Cleanup clears WorldGen.gen before late provider passes (SOTS).
	// The admitted pass sequence remains active until its own receipt is sealed.
	internal static bool InProgress => expected.Length > 0 && !Complete;
	internal static int CompletedCount => completed.Count;
	internal static int ExpectedCount => expected.Length;

	internal static void Reset()
	{
		expected = [];
		completed.Clear();
		Complete = false;
	}

	internal static void Arm(IEnumerable<string> names)
	{
		Reset();
		expected = names.ToArray();
		var missing = Required.Where(name => !expected.Contains(name)).ToArray();
		if (missing.Length != 0 || expected.Any(name =>
			name.StartsWith("DingDong Compiler:", StringComparison.Ordinal)))
			throw new InvalidOperationException("Native x10 pipeline is incomplete: " +
				string.Join(", ", missing));
	}

	internal static void Record(string name)
	{
		if (completed.Count >= expected.Length || expected[completed.Count] != name)
			throw new InvalidOperationException($"Native worldgen pass order changed at {name}.");
		completed.Add(name);
	}

	internal static void Seal()
	{
		if (expected.Length == 0 || !expected.SequenceEqual(completed))
			throw new InvalidOperationException("Native worldgen did not complete all admitted passes: " +
				string.Join(", ", expected.Skip(completed.Count)));
		Complete = true;
	}
}
