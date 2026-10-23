using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootProfileGate
{
	private const string OwnerName = "DingDongBiomeHomes";
	internal const int ProfileExternalCount = 64;
	private static readonly Version MinimumCompatibleOwnerVersion = new(0, 27, 16);
	private static readonly HardLootArtifactIdentity[] External =
	[
		.. HardLootExactManifestA.Entries,
		.. HardLootExactManifestB.Entries,
		.. HardLootExactManifestC.Entries.Where(entry => entry.Name != "PackWorldgenFix")
	];

	internal static bool Verified { get; private set; }
	// Owner-approved official Steam dependency, optional for the unchanged base
	// profile. No other additional mod is admitted through this exception.
	private static readonly HardLootArtifactIdentity Translation = new("CalamityRuTranslate", "3.2.1.152", "5bfc51ae-99b9-4668-a80b-bd87b808b402");
	private static bool HasTranslation => ModLoader.TryGetMod(Translation.Name, out _);
	internal static int ExpectedProfileCount => External.Length + 1 + (HasTranslation ? 1 : 0);
	internal static int ExpectedRuntimeExternalCount => Main.dedServ
		? External.Count(entry => !entry.ClientOnly) + (HasTranslation ? 1 : 0) : External.Length + (HasTranslation ? 1 : 0);
	internal static int DedicatedExcludedCount => External.Length + (HasTranslation ? 1 : 0) - ExpectedRuntimeExternalCount;
	internal static string EnabledProfileSha256 => HardLootEnabledProfileReceipt.Sha256;

	internal static void AssertExact(Mod owner)
	{
		bool compatibleOwnerVersion = owner.Version == new Version(0, 29, 5);
		if (owner.Name != OwnerName || !compatibleOwnerVersion)
			throw new InvalidOperationException(
				$"Hard-loot owner identity changed: {owner.Name} {owner.Version}; expected {OwnerName} " +
				$"{MinimumCompatibleOwnerVersion}, a compatible 0.27/0.28 patch release, or audited RC 0.29.0.1/0.29.0.2.");
		if (External.Length != ProfileExternalCount)
			throw new InvalidOperationException(
				$"Hard-loot manifest has {External.Length} external entries; expected {ProfileExternalCount}.");
		if (External.Count(entry => entry.ClientOnly) != 7)
			throw new InvalidOperationException("Hard-loot client-only manifest must contain exactly 7 entries.");

		HashSet<string> expectedProfile = External.Select(entry => entry.Name)
			.Append(OwnerName).ToHashSet(StringComparer.Ordinal);
		if (HasTranslation) expectedProfile.Add(Translation.Name);
		HardLootEnabledProfileReceipt.AssertExact(expectedProfile);
		Dictionary<string, Mod> loaded = ModLoader.Mods
			.Where(mod => mod.Name != "ModLoader")
			.ToDictionary(mod => mod.Name, StringComparer.Ordinal);
		HardLootArtifactIdentity[] runtimeExternal = External
			.Where(entry => !Main.dedServ || !entry.ClientOnly).ToArray();
		HashSet<string> expectedRuntime = runtimeExternal.Select(entry => entry.Name)
			.Append(OwnerName).ToHashSet(StringComparer.Ordinal);
		if (HasTranslation) expectedRuntime.Add(Translation.Name);
		string[] missing = expectedRuntime.Except(loaded.Keys).OrderBy(name => name).ToArray();
		string[] extra = loaded.Keys.Except(expectedRuntime).OrderBy(name => name).ToArray();
		if (missing.Length != 0 || extra.Length != 0)
			throw new InvalidOperationException(
				$"Hard-loot exact66 runtime names changed. Missing=[{string.Join(",", missing)}]; " +
				$"unexpected=[{string.Join(",", extra)}].");

		foreach (HardLootArtifactIdentity identity in runtimeExternal)
			AssertAssembly(loaded[identity.Name], identity);
		if (HasTranslation) AssertAssembly(loaded[Translation.Name], Translation);
		Verified = true;
	}

	private static void AssertAssembly(Mod mod, HardLootArtifactIdentity expected)
	{
		Guid actualMvid = mod.Code?.ManifestModule.ModuleVersionId ?? Guid.Empty;
		HardLootIdentityPolicy.Check(mod.Name, mod.Version.ToString(), actualMvid,
			expected.Version, expected.Mvid, message => mod.Logger.Warn(message));
	}

	internal static void Reset()
	{
		Verified = false;
		HardLootIdentityPolicy.Reset();
		HardLootEnabledProfileReceipt.Reset();
	}
}
