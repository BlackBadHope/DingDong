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
	private static void VerifyApprovalTransactionAndInitializeLedgers(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageHitCommit commitZero, out DamageHitLedger playerZeroClientLedger, out DamageHitLedger playerOneClientLedger, out DamageHitLedger replicatedServerLedger, out DamageEvaluationTrace p0Client1)
	{
		DamageHitLedger authorityCommitted = new();
		DamageHitLedger authorityTentative = authorityCommitted.Clone();
		Evaluate(engine, DamageProfileRegistry.Bullet, slime, authorityTentative, 2300, 2001, 1, attackerId: 0);
		DamageApprovalBook transactionBook = new(1);
		transactionBook.TryPrepare(0, intentZero, authorityTentative, 0.95f, 2300, 2400, DamageClass.Generic.Type);
		DamageEvaluationTrace beforeAuthorityCommit = Evaluate(engine, DamageProfileRegistry.Bullet, slime, authorityCommitted.Clone(), 2301, 2002, 1, attackerId: 0);
		Expect(failures, "approval-prepare-has-no-balance-sequence", beforeAuthorityCommit.IndependentHitsInSequence == 0);
		transactionBook.TryCommit(0, commitZero, 2301);
		transactionBook.TryConsume(0, 11, payloadZero, 2301, out DamagePreparedHit consumedTransaction);
		authorityCommitted.ReplaceWith(consumedTransaction.TentativeLedger);
		DamageEvaluationTrace afterAuthorityCommit = Evaluate(engine, DamageProfileRegistry.Bullet, slime, authorityCommitted, 2301, 2002, 1, attackerId: 0);
		Expect(failures, "approval-consume-has-no-balance-sequence", afterAuthorityCommit.IndependentHitsInSequence == 0);
		Expect(failures, "acquisition-tag-wire-roundtrip", RoundTripTag(DamageTags.CreatureDrop) == DamageTags.CreatureDrop);
		Expect(failures, "element-tag-wire-roundtrip", RoundTripTag(DamageTags.Astral) == DamageTags.Astral);
		playerZeroClientLedger = new();
		playerOneClientLedger = new();
		replicatedServerLedger = new();
		p0Client1 = Evaluate(engine, DamageProfileRegistry.Bullet, slime, playerZeroClientLedger, 1000, playerZeroAttack, 1, attackerId: 0);
	}
}
