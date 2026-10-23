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
	private static void VerifyTransactionFallbackAndClassifications(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageStrikePayload payloadOne, DamageHitCommit commitZero, DamageApprovalBook approvalBook)
	{
		Expect(failures, "approval-mismatch-closes-transaction", approvalBook.TryConsume(0, 11, payloadZero, 802, out _) == DamageApprovalResult.Missing);
		Expect(failures, "approval-player-one-isolated", approvalBook.TryConsume(1, 11, payloadOne, 802, out DamagePreparedHit consumedOne) == DamageApprovalResult.Consumed);
		Expect(failures, "approval-consumed-lineage-preserved", consumedOne?.Intent.AttackInstanceId == playerOneAttack);
		Expect(failures, "approval-replay-rejected", approvalBook.TryConsume(1, 11, payloadOne, 802, out _) == DamageApprovalResult.Missing);
		DamageApprovalBook missingCommitBook = new(1);
		missingCommitBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 810, 900, DamageClass.Generic.Type);
		Expect(failures, "approval-vanilla-before-commit-rejected", missingCommitBook.TryConsume(0, 11, payloadZero, 811, out _) == DamageApprovalResult.MissingCommit);
		Expect(failures, "approval-missing-stays-closed", !DamageAuthoritySystem.CanApplyVanillaDamage(DamageApprovalResult.Missing));
		Expect(failures, "approval-consumed-opens-vanilla-gate", DamageAuthoritySystem.CanApplyVanillaDamage(DamageApprovalResult.Consumed));
		DamageSourceClassification slimeStaff, finchStaff;
		VerifyReplicaIsolationAndRuntimeClassifications(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, payloadZero, commitZero, out slimeStaff, out finchStaff);
		Expect(failures, "runtime-slime-staff-acquisition", HasOnlyTag(slimeStaff.Profile.Acquisition, DamageTags.CreatureDrop));
		Expect(failures, "runtime-slime-staff-element", HasOnlyTag(slimeStaff.Profile.Element, DamageTags.Gel));
		Expect(failures, "runtime-finch-staff-acquisition", HasOnlyTag(finchStaff.Profile.Acquisition, DamageTags.Worldgen));
		Expect(failures, "runtime-finch-staff-element", HasOnlyTag(finchStaff.Profile.Element, DamageTags.Nature));
	}
}
