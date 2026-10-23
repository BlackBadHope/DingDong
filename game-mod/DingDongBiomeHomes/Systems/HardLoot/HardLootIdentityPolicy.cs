using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Systems.HardLoot;

// Reproducibility of an acceptance fixture is not runtime API compatibility.
internal static class HardLootIdentityPolicy
{
	private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);
	internal static bool ExactArtifacts { get; private set; } = true;
	internal static bool Strict =>
		Environment.GetEnvironmentVariable("DINGDONG_STRICT_PROFILE") == "1" ||
		Environment.GetEnvironmentVariable("DINGDONG_X10_MP_ACCEPTANCE") == "1" ||
		Environment.GetEnvironmentVariable("DINGDONG_X10_LOOT_ACCEPTANCE") == "1";

	internal static void Check(string name, string version, Guid mvid,
		string expectedVersion, string expectedMvid, Action<string> warn)
	{
		if (version == expectedVersion && mvid == Guid.Parse(expectedMvid)) return;
		ExactArtifacts = false;
		string detail = $"{name}: actual={version}/{mvid}; expected={expectedVersion}/{expectedMvid}";
		if (Strict) throw new InvalidOperationException("Hard-loot acceptance artifact changed: " + detail);
		if (Reported.Add(detail)) warn("[HardLootCompatibility] identity drift; " + detail +
			"; not proof of incompatibility; runtime API and patch checks remain enforced.");
	}

	internal static void Reset() { Reported.Clear(); ExactArtifacts = true; }
}
