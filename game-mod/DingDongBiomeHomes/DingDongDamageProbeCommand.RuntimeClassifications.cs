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
	private static void VerifyReplicaIsolationAndRuntimeClassifications(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageHitCommit commitZero, out DamageSourceClassification slimeStaff, out DamageSourceClassification finchStaff)
	{
		Expect(failures, "approval-expired-stays-closed", !DamageAuthoritySystem.CanApplyVanillaDamage(DamageApprovalResult.Expired));
		Expect(failures, "approval-missing-commit-stays-closed", !DamageAuthoritySystem.CanApplyVanillaDamage(DamageApprovalResult.MissingCommit));
		Expect(failures, "approval-payload-mismatch-stays-closed", !DamageAuthoritySystem.CanApplyVanillaDamage(DamageApprovalResult.PayloadMismatch));
		DamageHitLedger playerZeroClientLedger, playerOneClientLedger, replicatedServerLedger;
		VerifyApprovalCommitGuards(failures, intentZero, payloadZero, commitZero);
		DamageEvaluationTrace p0Client3, p0Server3, p1Client1, p1Server1;
		VerifyTwoPlayerReplicaParity(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, payloadZero, commitZero, out playerZeroClientLedger, out playerOneClientLedger, out replicatedServerLedger, out p0Client3, out p0Server3, out p1Client1, out p1Server1);
		ExpectMultiplier(failures, "player-zero-third-no-hidden-burst", p0Server3, 0.95d);
		Expect(failures, "player-one-first-client-server-parity", SameMultiplier(p1Client1, p1Server1));
		ExpectMultiplier(failures, "player-one-window-isolated", p1Server1, 0.95d);
		Expect(failures, "observer-receives-server-final-damage", FinalDamage(40, p0Client3) == FinalDamage(40, p0Server3));
		slimeStaff = DamageRuntimeClassifier.Classify(ContentSamples.ItemsByType[ItemID.SlimeStaff]);
		finchStaff = DamageRuntimeClassifier.Classify(ContentSamples.ItemsByType[ItemID.BabyBirdStaff]);
		Expect(failures, "runtime-slime-staff-delivery", HasOnlyTag(slimeStaff.Profile.Delivery, DamageTags.MinionProjectile));
	}
}
