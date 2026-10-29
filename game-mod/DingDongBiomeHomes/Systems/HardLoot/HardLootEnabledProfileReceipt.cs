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
	// The owner may run without the cosmetic music pack; every other name stays exact.
	private const string OptionalName = "CalamityModMusic";

	private static HashSet<string> captured;

	internal static string Sha256 { get; private set; } = string.Empty;

	internal static void AssertExact(HashSet<string> expected)
	{
		if (captured is null) Capture();
		string[] missing = expected.Except(captured).Where(name => name != OptionalName).OrderBy(name => name).ToArray();
		string[] extra = captured.Except(expected).OrderBy(name => name).ToArray();
		if (missing.Length != 0 || extra.Length != 0)
			throw new InvalidOperationException(
				$"Hard-loot exact66 enabled profile changed. Missing=[{string.Join(",", missing)}]; " +
				$"unexpected=[{string.Join(",", extra)}].");
	}

	private static void Capture()
	{
		string path = Path.Combine(ModLoader.ModPath, "enabled.json");
		byte[] bytes = File.ReadAllBytes(path);
		string[] names = JsonSerializer.Deserialize<string[]>(bytes) ??
			throw new InvalidOperationException("Hard-loot enabled profile is not a JSON string array.");
		captured = names.ToHashSet(StringComparer.Ordinal);
		if (captured.Count != names.Length)
			throw new InvalidOperationException(
				$"Hard-loot enabled profile has {names.Length} entries and {captured.Count} unique names.");
		Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
	}

	internal static void Reset()
	{
		captured = null;
		Sha256 = string.Empty;
	}
}
