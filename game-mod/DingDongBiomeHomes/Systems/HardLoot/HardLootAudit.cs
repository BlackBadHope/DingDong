using System;
using System.Globalization;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootAudit
{
	internal static int RulesVisited { get; private set; }
	internal static int WrappedLeaves { get; private set; }
	internal static int DifficultyBranches { get; private set; }
	internal static int UltraRarePreserved { get; private set; }
	internal static int DropNothingBranchesPreserved { get; private set; }
	internal static int GuaranteedRulesPreserved { get; private set; }
	internal static int NormalizedRulesPreserved { get; private set; }
	internal static int ProtectedRules { get; private set; }
	internal static int UnsupportedRules { get; private set; }
	internal static int ReportFailures { get; private set; }
	internal static int DifficultyBoostsNeutralized { get; private set; }
	internal static int LivingReplaysBlocked { get; private set; }
	internal static int ProviderReplaysBlocked { get; private set; }
	internal static int LootBonusDrifts { get; private set; }
	internal static int RandomLootDrifts { get; private set; }

	internal static void Bind(Mod mod) => HardLootAuditDetails.Bind(mod);
	internal static void Visited() => RulesVisited++;
	internal static void Wrapped() => WrappedLeaves++;
	internal static void Difficulty(int branches) => DifficultyBranches += branches;
	internal static void UltraRare() => UltraRarePreserved++;
	internal static void DropNothingBranchPreserved() => DropNothingBranchesPreserved++;
	internal static void GuaranteedPreserved() => GuaranteedRulesPreserved++;
	internal static void NormalizedPreserved() => NormalizedRulesPreserved++;
	internal static void Protected() => ProtectedRules++;

	internal static void DifficultyRate(string selector, int itemId,
		float baseline, float harder, float final)
	{
		if (harder <= baseline + 0.0000001f) return;
		DifficultyBoostsNeutralized++;
		float bonus = (harder / baseline - 1f) * 100f;
		HardLootAuditDetails.Difficulty(string.Format(CultureInfo.InvariantCulture,
			"{0}/item:{1}/bonus:{2:F2}%/final:{3:P4}", selector, itemId, bonus, final));
	}

	internal static void Unsupported(string scope, string type, string items)
	{
		UnsupportedRules++;
		HardLootAuditDetails.Coverage($"{scope}/type:{type}/items:[{items}]");
	}

	internal static void ReportFailed(string scope, string type)
	{
		ReportFailures++;
		HardLootAuditDetails.Coverage($"{scope}/type:{type}/droprate-report-failed");
	}

	internal static void LivingReplayBlocked(string surface)
	{
		LivingReplaysBlocked++;
		HardLootAuditDetails.RuntimeNotice("living-" + surface);
	}

	internal static void ProviderReplayBlocked(string provider)
	{
		ProviderReplaysBlocked++;
		HardLootAuditDetails.RuntimeNotice(provider + "-full-replay");
	}

	internal static void LootBonusDrift()
	{
		LootBonusDrifts++;
		HardLootAuditDetails.RuntimeNotice("LootBonus-config-drift-neutralized");
	}

	internal static void RandomLootDrift()
	{
		RandomLootDrifts++;
		HardLootAuditDetails.RuntimeNotice("RandomLoot-config-drift-neutralized");
	}

	internal static void Emit(Mod owner)
	{
		HardLootRuntimeGuard.AssertCurrent();
		HardLootAuditDetails.Emit(owner);
		bool failed = RulesVisited == 0 || DropNothingBranchesPreserved == 0 ||
			GuaranteedRulesPreserved == 0 || UnsupportedRules != 0 || ReportFailures != 0 ||
			!CalamityPityItemLootAdapter.Verified ||
			!ThoriumMultipleItemLootAdapter.Verified ||
			!AndroLibBagCoverage.Verified ||
			!HardLootProfileGate.Verified || !HardLootProviderGate.Verified ||
			!LootBonusNeutralizer.IsNeutralized || !RandomLootGate.IsDisabled ||
			!LivingNpcLootReplayGuard.Installed || !NpcLootReentryGuard.Installed ||
			!FargoSoulsReplayGuard.Installed;
		if (failed)
			throw new InvalidOperationException(
				"Hard-loot final policy gate refused partial coverage. " + HardLootAuditDetails.CoverageSummary);
	}

	internal static void EmitRuntimeSummary() => HardLootAuditDetails.EmitRuntimeSummary();

	internal static void Reset()
	{
		RulesVisited = WrappedLeaves = DifficultyBranches = UltraRarePreserved = 0;
		DropNothingBranchesPreserved = GuaranteedRulesPreserved = NormalizedRulesPreserved = 0;
		ProtectedRules = UnsupportedRules = ReportFailures = DifficultyBoostsNeutralized = 0;
		LivingReplaysBlocked = ProviderReplaysBlocked = LootBonusDrifts = RandomLootDrifts = 0;
		HardLootAuditDetails.Reset();
	}
}
