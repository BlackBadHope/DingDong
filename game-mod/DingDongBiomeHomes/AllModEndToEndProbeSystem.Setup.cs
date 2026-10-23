using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed partial class AllModEndToEndProbeSystem
{
	private SetupSnapshot AuditLoadedMods(bool registerCalls, bool runDamage)
	{
		try
		{
			SetupAuditContext context = CreateSetupAudit(runDamage);
			foreach (string name in context.Expected)
				AuditProvider(context, name, registerCalls, runDamage);
			return CompleteSetupAudit(context);
		}
		catch (Exception exception)
		{
			string marker = $"DINGDONG_ALLMOD_E2E_SETUP|verdict=FAIL|exception={exception.GetType().Name}|message={OneLine(exception.Message)}";
			Mod.Logger.Error(marker, exception);
			return SetupSnapshot.Failure(marker);
		}
	}

	private SetupAuditContext CreateSetupAudit(bool runDamage)
	{
		string[] expected = ExpectedMods();
		string[] expectedServer = ExpectedServerMods();
		HashSet<string> serverSet = expectedServer.ToHashSet(StringComparer.Ordinal);
		string[] loaded = ModLoader.Mods.Select(mod => mod.Name).Where(name => name != "ModLoader").OrderBy(name => name, StringComparer.Ordinal).ToArray();
		int excluded = expected.Count(name => !serverSet.Contains(name));
		bool exact = expected.Length == Systems.HardLoot.HardLootProfileGate.ExpectedProfileCount && expected.Distinct(StringComparer.Ordinal).Count() == expected.Length && expectedServer.Length > 0 && expectedServer.Distinct(StringComparer.Ordinal).Count() == expectedServer.Length && expectedServer.All(name => expected.Contains(name, StringComparer.Ordinal)) && expectedServer.SequenceEqual(loaded, StringComparer.Ordinal) && excluded == expected.Length - expectedServer.Length;
		string damageProtocol = "SKIPPED";
		bool damagePassed = !runDamage;
		if (runDamage)
		{
			string damage = DingDongDamageProbeCommand.RunProbe(Mod, "DingDongAllModE2E-prepare");
			damagePassed = damage.Contains("|verdict=PASS|", StringComparison.Ordinal);
			damageProtocol = damagePassed ? "PASS" : "FAIL";
		}

		SetupAuditContext context = new() { Expected = expected, ExpectedServer = expectedServer, ServerSet = serverSet, Loaded = loaded, DedicatedExcluded = excluded, Exact = exact, DamagePassed = damagePassed, DamageProtocol = damageProtocol, Templates = QuestContentRegistry.SnapshotTemplates() };
		if (!exact)
			context.Failures.Add($"mod-set:enabled={expected.Length},serverExpected={expectedServer.Length},loaded={loaded.Length},excluded={excluded}");
		if (!damagePassed)
			context.Failures.Add("damage-protocol");
		return context;
	}

	private SetupSnapshot CompleteSetupAudit(SetupAuditContext context)
	{
		bool passed = context.Exact && context.Failures.Count == 0;
		string marker = $"DINGDONG_ALLMOD_E2E_SETUP|verdict={(passed ? "PASS" : "FAIL")}|enabled={context.Expected.Length}/66|loaded={context.Loaded.Length}/{context.ExpectedServer.Length}|dedicatedExcluded={context.DedicatedExcluded}|exactModSet={context.Exact}|damageProtocol={context.DamageProtocol}|modCalls={context.CallsPassed}/{context.ExpectedServer.Length}|projectileHooks={context.ProjectileHooksPassed}/{context.ProjectileHooksEligible}|failures={OneLine(string.Join(',', context.Failures))}";
		if (passed) Mod.Logger.Info(marker); else Mod.Logger.Error(marker);
		return new SetupSnapshot(passed, context.Exact, context.Loaded.Length, context.Expected.Length, context.ExpectedServer.Length, context.DedicatedExcluded, context.CallsPassed, context.ProjectileHooksEligible, context.ProjectileHooksPassed, context.DamageProtocol, context.Mods, marker);
	}
}
