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
	private static void PrepareProtocolTransactions(List<string> failures, ulong playerZeroAttack, ulong playerOneAttack, DamageHitIntent intentZero, DamageHitIntent intentOne, out DamageStrikePayload payloadZero, out DamageStrikePayload payloadOne, out DamageHitCommit commitZero, out DamageHitCommit commitOne, out DamageApprovalBook approvalBook)
	{
		Expect(failures, "packet-intent-player-zero-roundtrip", RoundTripIntent(intentZero) == intentZero);
		Expect(failures, "packet-intent-player-one-roundtrip", RoundTripIntent(intentOne) == intentOne);
		DamageHitReceipt receipt = new(true, 11, playerOneAttack, 1.18f, DamageReceiptReason.Accepted);
		Expect(failures, "packet-receipt-roundtrip", RoundTripReceipt(receipt) == receipt);
		Expect(failures, "packet-version-fail-closed", RejectsUnknownProtocolVersion());
		payloadZero = new(42, 50, DamageClass.Generic.Type, 1, 2.5f, true, false, false);
		payloadOne = new(117, 80, DamageClass.Magic.Type, -1, 0.75f, false, false, true);
		commitZero = new(DamageSourceKind.Item, 11, 1309, -1, playerZeroAttack, 1, payloadZero);
		commitOne = new(DamageSourceKind.Projectile, 11, 1, 903, playerOneAttack, 17, payloadOne);
		Expect(failures, "packet-commit-player-zero-roundtrip", RoundTripCommit(commitZero) == commitZero);
		Expect(failures, "packet-commit-player-one-roundtrip", RoundTripCommit(commitOne) == commitOne);
		Expect(failures, "strike-payload-wire-roundtrip", RoundTripStrikePayload(payloadZero).MatchesExactly(payloadZero));
		Expect(failures, "strike-payload-unknown-flags-fail-closed", RejectsUnknownStrikeFlags());
		approvalBook = new(2);
		Expect(failures, "approval-prepare-player-zero", approvalBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 800, 900, DamageClass.Generic.Type) == DamageApprovalResult.Prepared);
		Expect(failures, "approval-prepare-player-one", approvalBook.TryPrepare(1, intentOne, new DamageHitLedger(), 1.18f, 800, 900, DamageClass.Magic.Type) == DamageApprovalResult.Prepared);
		Expect(failures, "approval-two-player-slots", approvalBook.Count == 2);
	}

	private static void ExpectMultiplier(List<string> failures, string name, DamageEvaluationTrace trace, double expected) => Expect(failures, name, Nearly(trace.FinalMultiplier, expected));
	private static void Expect(List<string> failures, string name, bool condition)
	{
		if (!condition)
			failures.Add(name);
	}
}
