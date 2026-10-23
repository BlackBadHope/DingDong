using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

public sealed class HardLootSystem : ModSystem
{
	private bool initialized;

	public override void Load()
	{
		ResetState();
		try
		{
			HardLootProfileGate.AssertExact(Mod);
			HardLootProviderGate.AssertExact();
			RandomLootGate.AssertNpcRandomizersDisabled();
			FargoSoulsDifficultyAdapter.Initialize();
			HardLootProviderRuleAdapters.Initialize();
			HardLootAudit.Bind(Mod);
			DevianttGiftPolicy.Install();
			FargoSoulsReplayGuard.Install();
			LivingNpcLootReplayGuard.Install();
			NpcLootReentryGuard.Install();
			initialized = true;
		}
		catch (Exception loadFailure)
		{
			RollbackOrAggregate(loadFailure);
			throw;
		}
	}

	public override void PostSetupContent()
	{
		try
		{
			HardLootProfileGate.AssertExact(Mod);
			LootBonusNeutralizer.Apply();
		}
		catch (Exception setupFailure)
		{
			RollbackOrAggregate(setupFailure);
			throw;
		}
	}

	public override void PostSetupRecipes()
	{
		try
		{
			HardLootProfileGate.AssertExact(Mod);
			HardLootProviderRuleAdapters.NormalizeDeferred();
			HardLootAudit.Emit(Mod);
		}
		catch (Exception auditFailure)
		{
			RollbackOrAggregate(auditFailure);
			throw;
		}
	}

	public override void Unload()
	{
		List<Exception> failures = [];
		if (initialized) Attempt(HardLootAudit.EmitRuntimeSummary, failures);
		Attempt(Cleanup, failures);
		if (failures.Count != 0)
			throw new AggregateException("Hard-loot unload failed.", failures);
	}

	private static void ResetState()
	{
		HardLootAudit.Reset();
		HardLootProfileGate.Reset();
		HardLootProviderGate.Reset();
		RandomLootGate.Reset();
	}

	private void RollbackOrAggregate(Exception original)
	{
		try { Cleanup(); }
		catch (Exception cleanupFailure)
		{
			throw new AggregateException("Hard-loot initialization and rollback both failed.",
				original, cleanupFailure);
		}
	}

	private void Cleanup()
	{
		List<Exception> failures = [];
		Attempt(NpcLootReentryGuard.Uninstall, failures);
		Attempt(LivingNpcLootReplayGuard.Uninstall, failures);
		Attempt(FargoSoulsReplayGuard.Uninstall, failures);
		Attempt(DevianttGiftPolicy.Uninstall, failures);
		Attempt(HardLootProviderRuleAdapters.RestoreAndReset, failures);
		Attempt(FargoSoulsDifficultyAdapter.Reset, failures);
		Attempt(LootBonusNeutralizer.RestoreAndReset, failures);
		Attempt(RandomLootGate.Reset, failures);
		Attempt(HardLootProviderGate.Reset, failures);
		Attempt(HardLootProfileGate.Reset, failures);
		Attempt(HardLootAudit.Reset, failures);
		initialized = false;
		if (failures.Count != 0)
			throw new AggregateException("Hard-loot cleanup failed.", failures);
	}

	private static void Attempt(Action action, List<Exception> failures)
	{
		try { action(); }
		catch (Exception error) { failures.Add(error); }
	}
}
