using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private sealed class SetupAuditContext
	{
		internal string[] Expected { get; init; }
		internal string[] ExpectedServer { get; init; }
		internal HashSet<string> ServerSet { get; init; }
		internal string[] Loaded { get; init; }
		internal int DedicatedExcluded { get; init; }
		internal bool Exact { get; init; }
		internal bool DamagePassed { get; init; }
		internal string DamageProtocol { get; init; }
		internal IReadOnlyList<UniversalQuestTemplate> Templates { get; init; }
		internal List<string> Failures { get; } = new();
		internal List<ModSnapshot> Mods { get; } = new();
		internal int CallsPassed;
		internal int ProjectileHooksEligible;
		internal int ProjectileHooksPassed;
	}

	private readonly record struct ModSnapshot(string Name, bool ServerApplicable, bool Loaded, int Items, int Projectiles, int HostileNpcs, int Bosses, int TownNpcs, int Buffs, int Biomes, int QuestTemplates, bool DamagePassed, string ProjectileHook, bool CallPassed)
	{
		internal static ModSnapshot DedicatedExcluded(string name) => new(name, false, false, 0, 0, 0, 0, 0, 0, 0, 0, true, "N/A", true);
		internal static ModSnapshot ServerMissing(string name) => new(name, true, false, 0, 0, 0, 0, 0, 0, 0, 0, false, "FAIL", false);
	}

	private readonly record struct SetupSnapshot(bool Passed, bool ExactModSet, int LoadedCount, int EnabledCount, int ServerExpectedCount, int DedicatedExcludedCount, int CallPassed, int ProjectileHooksEligible, int ProjectileHooksPassed, string DamageProtocol, IReadOnlyList<ModSnapshot> Mods, string Marker)
	{
		internal static SetupSnapshot Failure(string marker) => new(false, false, 0, 0, 0, 0, 0, 0, 0, "FAIL", Array.Empty<ModSnapshot>(), marker);
	}

	private readonly record struct QuestHookSnapshot(bool Passed, int EligibleCases, int PassedCases, IReadOnlyDictionary<string, int> EligibleByMod, IReadOnlyDictionary<string, int> PassedByMod);
}
