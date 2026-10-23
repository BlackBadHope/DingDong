using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Non-mutating dedicated-server contract probe for the deterministic damage
/// core, persistent affinity metadata and multiplayer authority protocol.
/// </summary>
public sealed partial class DingDongDamageProbeCommand
{
	private static void RunCoverageAndUiAudits(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageStrikePayload payloadOne, DamageHitCommit commitZero, DamageHitCommit commitOne, DamageApprovalBook approvalBook, out DamageCoverageSnapshot coverage, out DamageExhaustiveAuditResult exhaustive, out DamageUiAuditResult uiAudit, out DamageGuideAuditResult guideAudit, out DamageDifficultySnapshot difficulty)
	{
		coverage = VerifyApprovalConsumptionAndContentCoverage(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, payloadZero, payloadOne, commitZero, commitOne, approvalBook);
		Expect(failures, "coverage-dot-present", coverage.DamageOverTimeEffects > 0);
		Expect(failures, "coverage-trap-projectiles-present", coverage.TrapProjectiles > 0);
		Expect(failures, "coverage-environment-adapter", coverage.EnvironmentAdapters == 1);
		exhaustive = DamageExhaustiveAudit.Run(engine);
		uiAudit = DamageUiAudit.Run();
		guideAudit = DamageGuideAudit.Run();
		difficulty = DamageDifficultyContextSystem.Capture();
		Expect(failures, "exhaustive-item-count", exhaustive.Items == coverage.Items);
		Expect(failures, "exhaustive-projectile-count", exhaustive.Projectiles == coverage.Projectiles);
		Expect(failures, "exhaustive-target-count", exhaustive.HostileNpcs == coverage.HostileNpcs);
		Expect(failures, "exhaustive-dot-count", exhaustive.DamageOverTimeEffects == coverage.DamageOverTimeEffects);
		Expect(failures, "exhaustive-item-pairs", exhaustive.ItemPairs == exhaustive.ExpectedItemPairs);
		Expect(failures, "exhaustive-projectile-pairs", exhaustive.ProjectilePairs == exhaustive.ExpectedProjectilePairs);
		Expect(failures, "exhaustive-dot-pairs", exhaustive.DamageOverTimePairs == exhaustive.ExpectedDamageOverTimePairs);
		Expect(failures, "exhaustive-no-failures", exhaustive.Failures == 0);
		Expect(failures, "ui-audit-all-items-labeled", uiAudit.MissingLabels == 0);
		Expect(failures, "ui-audit-balanced-profiles", uiAudit.OutOfRangeProfiles == 0);
	}
}
