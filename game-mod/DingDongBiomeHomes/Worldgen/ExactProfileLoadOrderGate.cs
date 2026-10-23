using System;
using System.Linq;
using DingDongBiomeHomes.Systems.HardLoot;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal static class ExactProfileLoadOrderGate
{
	private static readonly string[] PassiveTail = ["PackBuilder"];
	private static string receipt;

	internal static bool Verified { get; private set; }

	internal static void AssertLast(Mod owner)
	{
		try
		{
			HardLootProfileGate.AssertExact(owner);
			Mod[] loaded = ModLoader.Mods
				.Where(mod => mod.Name != "ModLoader").ToArray();
			int runtimeExternal = HardLootProfileGate.ExpectedRuntimeExternalCount;
			if (loaded.Length != runtimeExternal + 1)
				throw new InvalidOperationException(
					$"Exact profile contains {loaded.Length} runtime mods; expected {runtimeExternal + 1}.");
			int ownerIndex = Array.FindIndex(loaded, mod => mod.Name == owner.Name);
			string[] tail = loaded.Skip(ownerIndex + 1).Select(mod => mod.Name).ToArray();
			if (ownerIndex != loaded.Length - PassiveTail.Length - 1 ||
				!tail.SequenceEqual(PassiveTail, StringComparer.Ordinal))
				throw new InvalidOperationException(
					$"DingDong must immediately precede the exact passive tail: " +
					$"ownerIndex={ownerIndex}; tail=[{string.Join(",", tail)}].");
			receipt = $"profileExternal={HardLootProfileGate.ProfileExternalCount}|" +
				$"runtimeExternal={runtimeExternal}|" +
				$"dedicatedExcluded={HardLootProfileGate.DedicatedExcludedCount}|" +
				$"ownerIndex={ownerIndex}|passiveTail={string.Join(",", tail)}|" +
				$"tailCount={tail.Length}|last={loaded[^1].Name}";
			Verified = true;
			owner.Logger.Info(
				$"DINGDONG_EXACT65_LOAD_ORDER|verdict=PASS|{receipt}");
		}
		catch (Exception exception)
		{
			Verified = false;
			receipt = string.Empty;
			owner.Logger.Error(
				$"DINGDONG_EXACT65_LOAD_ORDER|verdict=WITHHELD|reason={exception.GetType().Name}");
			throw;
		}
	}

	internal static bool TryRead(out string value)
	{
		value = receipt;
		return Verified && !string.IsNullOrEmpty(receipt);
	}

	internal static void Reset()
	{
		Verified = false;
		receipt = string.Empty;
	}
}
