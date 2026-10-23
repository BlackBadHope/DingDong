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
	private static void RunProtocolCoverageChecks(List<string> failures, DamageRuleEngine engine, DamageTargetProfile slime, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageHitIntent intentOne, out DamageCoverageSnapshot coverage, out DamageExhaustiveAuditResult exhaustive, out DamageUiAuditResult uiAudit, out DamageGuideAuditResult guideAudit, out DamageDifficultySnapshot difficulty)
	{
		DamageStrikePayload payloadZero, payloadOne;
		DamageHitCommit commitZero, commitOne;
		DamageApprovalBook approvalBook;
		PrepareProtocolTransactions(failures, playerZeroAttack, playerOneAttack, intentZero, intentOne, out payloadZero, out payloadOne, out commitZero, out commitOne, out approvalBook);
		Expect(failures, "approval-commit-player-zero", approvalBook.TryCommit(0, commitZero, 801) == DamageApprovalResult.Committed);
		RunCoverageAndUiAudits(failures, engine, slime, playerZeroAttack, playerOneAttack, intentZero, payloadZero, payloadOne, commitZero, commitOne, approvalBook, out coverage, out exhaustive, out uiAudit, out guideAudit, out difficulty);
		Expect(failures, "ui-audit-no-stateful-burst", uiAudit.StatefulBurstProfiles == 0);
		Expect(failures, "guide-all-pages-present", guideAudit.Pages == 8);
		Expect(failures, "guide-all-pages-populated", guideAudit.EmptyPages == 0);
		Expect(failures, "guide-difficulty-adapters-healthy", guideAudit.DifficultyAdapterFailures == 0);
		DamageTargetProfile phaseTarget = new("dingdong:target/probe_phase", new[] { "dingdong:morphology/probe" }, phaseMultipliers: new Dictionary<DamagePhaseRole, double> { [DamagePhaseRole.Critical] = 1.10d });
		DamageEvaluationTrace phaseTrace = engine.EvaluateAcceptedHit(DamageProfileRegistry.KineticContact, phaseTarget, new DamageHitObservation(2100, 7, 11, 991, 1, false), new DamageHitLedger(), phaseRole: DamagePhaseRole.Critical);
		VerifyPhaseLedgerAndSegmentClassification(failures, engine, slime, phaseTrace);
	}

	private static DamageStrikePayload RoundTripStrikePayload(DamageStrikePayload payload)
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		DamageNetworkCodec.WriteStrikePayload(writer, payload);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		return DamageNetworkCodec.ReadStrikePayload(reader);
	}
}
