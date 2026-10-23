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
	private static void VerifyApprovalCommitGuards(List<string> failures, DamageHitIntent intentZero, DamageStrikePayload payloadZero, DamageHitCommit commitZero)
	{
		DamageApprovalBook identityBook = new(1);
		identityBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 810, 900, DamageClass.Generic.Type);
		Expect(failures, "approval-commit-identity-mismatch-rejected", identityBook.TryCommit(0, commitZero with { TargetId = 12 }, 811) == DamageApprovalResult.IdentityMismatch);
		DamageApprovalBook duplicateCommitBook = new(1);
		duplicateCommitBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 810, 900, DamageClass.Generic.Type);
		Expect(failures, "approval-duplicate-first-commit", duplicateCommitBook.TryCommit(0, commitZero, 811) == DamageApprovalResult.Committed);
		Expect(failures, "approval-duplicate-commit-rejected", duplicateCommitBook.TryCommit(0, commitZero, 811) == DamageApprovalResult.DuplicateCommit);
		Expect(failures, "approval-duplicate-closes-transaction", duplicateCommitBook.TryConsume(0, 11, payloadZero, 812, out _) == DamageApprovalResult.Missing);
		DamageApprovalBook expiredBook = new(1);
		expiredBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 810, 811, DamageClass.Generic.Type);
		Expect(failures, "approval-expired-commit-rejected", expiredBook.TryCommit(0, commitZero, 812) == DamageApprovalResult.Expired);
		DamageApprovalBook damageTypeBook = new(1);
		damageTypeBook.TryPrepare(0, intentZero, new DamageHitLedger(), 0.95f, 810, 900, DamageClass.Generic.Type);
		Expect(failures, "approval-server-damage-type-mismatch-rejected", damageTypeBook.TryCommit(0, commitZero with { Payload = payloadZero with { DamageType = DamageClass.Magic.Type } }, 811) == DamageApprovalResult.PayloadMismatch);
		VerifyPayloadAuthorityGuards(failures, intentZero, payloadZero, commitZero);
	}
}
