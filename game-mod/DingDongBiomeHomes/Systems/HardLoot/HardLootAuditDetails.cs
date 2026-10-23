using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootAuditDetails
{
	private static readonly List<string> DifficultyExamples = [];
	private static readonly List<string> CoverageFailures = [];
	private static readonly HashSet<string> RuntimeNotices = [];
	private static Mod owner;

	internal static string CoverageSummary => CoverageFailures.Count == 0
		? "coverageFailures=[]" : $"coverageFailures=[{string.Join(";", CoverageFailures)}]";

	internal static void Bind(Mod mod) => owner = mod;
	internal static void Difficulty(string detail)
	{
		if (DifficultyExamples.Count < 12) DifficultyExamples.Add(detail);
	}

	internal static void Coverage(string detail)
	{
		if (CoverageFailures.Count < 24) CoverageFailures.Add(detail);
	}

	internal static void RuntimeNotice(string surface)
	{
		if (RuntimeNotices.Add(surface))
			owner?.Logger.Warn($"[HardLootRuntime] event={surface}; later repeats are counted silently.");
	}

	internal static void Emit(Mod logger)
	{
		string role = Main.dedServ ? "server" : Main.netMode == NetmodeID.MultiplayerClient ? "client" : "singleplayer";
		logger.Logger.Info(
			$"[HardLootAudit] role={role}; profile={(HardLootIdentityPolicy.Strict ? "exact66-enabled+runtime-mvid" : "runtime-contracts")}; " +
			$"profileExternal={HardLootProfileGate.ProfileExternalCount}; " +
			$"runtimeExternal={HardLootProfileGate.ExpectedRuntimeExternalCount}; " +
			$"dedicatedExcluded={HardLootProfileGate.DedicatedExcludedCount}; " +
			$"enabledProfileSha256={HardLootProfileGate.EnabledProfileSha256}; " +
			$"artifactGate={HardLootProfileGate.Verified}; " +
			$"exactArtifacts={HardLootIdentityPolicy.ExactArtifacts}; strictIdentity={HardLootIdentityPolicy.Strict}; " +
			$"lootBonusNeutralized={LootBonusNeutralizer.IsNeutralized}; rulesVisited={HardLootAudit.RulesVisited}; " +
			$"wrappedLeaves={HardLootAudit.WrappedLeaves}; normalizedDifficultyBranches={HardLootAudit.DifficultyBranches}; " +
			$"difficultyBoostsNeutralized={HardLootAudit.DifficultyBoostsNeutralized}; " +
			$"ultraRarePreserved={HardLootAudit.UltraRarePreserved}; " +
			$"dropNothingBranchesPreserved={HardLootAudit.DropNothingBranchesPreserved}; " +
			$"guaranteedRulesPreserved={HardLootAudit.GuaranteedRulesPreserved}; " +
			$"normalizedRulesPreserved={HardLootAudit.NormalizedRulesPreserved}; " +
			$"protectedRules={HardLootAudit.ProtectedRules}; " +
			$"unsupportedRules={HardLootAudit.UnsupportedRules}; reportFailures={HardLootAudit.ReportFailures}; " +
			$"itemLootPityRoots={CalamityPityItemLootAdapter.Roots}; " +
			$"itemLootPityCoverage={CalamityPityItemLootAdapter.Verified}; " +
			$"thoriumItemLootRoots={ThoriumMultipleItemLootAdapter.Roots}; " +
			$"thoriumItemLootCoverage={ThoriumMultipleItemLootAdapter.Verified}; " +
			$"androBagRoots={AndroLibBagCoverage.Roots}; " +
			$"androBagExpected={AndroLibBagCoverage.Expected}; " +
			$"androBagCoverage={AndroLibBagCoverage.Verified}; " +
			$"providerGate={HardLootProviderGate.Verified}; livingGuard={LivingNpcLootReplayGuard.Installed}; " +
			$"randomLootDisabled={RandomLootGate.IsDisabled}; reentryGuard={NpcLootReentryGuard.Installed}; " +
			$"fargoReplayGuard={FargoSoulsReplayGuard.Installed}; examples=[{string.Join(";", DifficultyExamples)}]; " +
			CoverageSummary);
	}

	internal static void EmitRuntimeSummary() => owner?.Logger.Info(
		$"[HardLootRuntime] livingReplaysBlocked={HardLootAudit.LivingReplaysBlocked}; " +
		$"providerReplaysBlocked={HardLootAudit.ProviderReplaysBlocked}; " +
		$"lootBonusDrifts={HardLootAudit.LootBonusDrifts}; randomLootDrifts={HardLootAudit.RandomLootDrifts}");

	internal static void Reset()
	{
		DifficultyExamples.Clear();
		CoverageFailures.Clear();
		RuntimeNotices.Clear();
		owner = null;
	}
}
