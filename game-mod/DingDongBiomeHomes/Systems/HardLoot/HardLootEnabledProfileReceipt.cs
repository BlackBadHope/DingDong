using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootEnabledProfileReceipt
{
	private static HashSet<string> captured;

	internal static string Sha256 { get; private set; } = string.Empty;

	internal static void AssertExact(HashSet<string> expected)
	{
		if (captured is null) Capture(expected);
		string[] missing = expected.Except(captured).OrderBy(name => name).ToArray();
		string[] extra = captured.Except(expected).OrderBy(name => name).ToArray();
		if (missing.Length != 0 || extra.Length != 0)
			throw new InvalidOperationException(
				$"Hard-loot exact66 enabled profile changed. Missing=[{string.Join(",", missing)}]; " +
				$"unexpected=[{string.Join(",", extra)}].");
	}

	private static void Capture(HashSet<string> expected)
	{
		string path = Path.Combine(ModLoader.ModPath, "enabled.json");
		byte[] bytes = File.ReadAllBytes(path);
		string[] names = JsonSerializer.Deserialize<string[]>(bytes) ??
			throw new InvalidOperationException("Hard-loot enabled profile is not a JSON string array.");
		captured = names.ToHashSet(StringComparer.Ordinal);
		if (names.Length != expected.Count || captured.Count != names.Length)
			throw new InvalidOperationException(
				$"Hard-loot enabled profile has {names.Length} entries and {captured.Count} unique names; " +
				$"expected {expected.Count} unique names.");
		Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
	}

	internal static void Reset()
	{
		captured = null;
		Sha256 = string.Empty;
	}
}
