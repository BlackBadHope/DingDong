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
	private static void VerifyPhaseLedgerAndSegmentClassification(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, DamageEvaluationTrace phaseTrace)
	{
		ExpectMultiplier(failures, "critical-phase-multiplier", phaseTrace, 1.10d);
		Expect(failures, "critical-phase-trace", Nearly(phaseTrace.PhaseMultiplier, 1.10d));
		DamageHitLedger transactionLedger = new();
		Evaluate(engine, DamageProfileRegistry.Bullet, slime, transactionLedger, 2200, 1001, 1);
		DamageHitLedger rejectedCandidateLedger = transactionLedger.Clone();
		Evaluate(engine, DamageProfileRegistry.Bullet, slime, rejectedCandidateLedger, 2201, 1002, 1);
		DamageEvaluationTrace originalSecond = Evaluate(engine, DamageProfileRegistry.Bullet, slime, transactionLedger, 2201, 1003, 1);
		Expect(failures, "rejected-ledger-candidate-isolated", originalSecond.IndependentHitsInSequence == 0);
		DamageHitLedger committedLedger = transactionLedger.Clone();
		DamageEvaluationTrace committedThird = Evaluate(engine, DamageProfileRegistry.Bullet, slime, committedLedger, 2202, 1004, 1);
		transactionLedger.ReplaceWith(committedLedger);
		ExpectMultiplier(failures, "committed-ledger-candidate-applied-without-burst", committedThird, 0.95d);
		NPC segmentProbe = new()
		{
			active = true,
			whoAmI = 8,
			realLife = 3,
			life = 100,
			lifeMax = 100,
			aiStyle = NPCAIStyleID.Slime
		};
		DamageTargetClassification segmentClassification = DamageRuntimeClassifier.Classify(segmentProbe);
		Expect(failures, "segment-shared-encounter-root", segmentClassification.EncounterRootId == 3);
		Expect(failures, "segment-role-body", segmentClassification.SegmentRole == DamageSegmentRole.Body);
		Expect(failures, "segment-opening-phase", segmentClassification.PhaseRole == DamagePhaseRole.Opening);
	}
}
