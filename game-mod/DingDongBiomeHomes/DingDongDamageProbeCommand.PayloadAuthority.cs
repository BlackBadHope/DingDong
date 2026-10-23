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
	private static void VerifyPayloadAuthorityGuards(List<string> failures, DamageHitIntent intent, DamageStrikePayload payload, DamageHitCommit commit)
	{
		DamagePayloadEnvelope envelope = new(DamageClass.Generic.Type, payload.SourceDamage, payload.Damage, payload.Knockback, payload.Crit, false, false);
		VerifyPayloadRejection(failures, "payload-authority-huge-damage", intent, payload, commit, envelope, payload with { Damage = int.MaxValue });
		VerifyPayloadRejection(failures, "payload-authority-excessive-source", intent, payload, commit, envelope, payload with { SourceDamage = payload.SourceDamage + 1 });
		VerifyPayloadRejection(failures, "payload-authority-excessive-knockback", intent, payload, commit, envelope, payload with { Knockback = payload.Knockback + 0.01f });
		VerifyPayloadRejection(failures, "payload-authority-unauthorized-instant-kill", intent, payload, commit, envelope, payload with { InstantKill = true });
	}

	private static void VerifyPayloadRejection(List<string> failures, string marker, DamageHitIntent intent, DamageStrikePayload acceptedPayload, DamageHitCommit commit, DamagePayloadEnvelope envelope, DamageStrikePayload forgedPayload)
	{
		DamageApprovalBook book = new(1);
		Expect(failures, marker + "-prepare", book.TryPrepare(0, intent, new DamageHitLedger(), 0.95f, 810, 900, DamageClass.Generic.Type, envelope) == DamageApprovalResult.Prepared);
		Expect(failures, marker + "-rejected", book.TryCommit(0, commit with { Payload = forgedPayload }, 811) == DamageApprovalResult.PayloadMismatch);
		DamageApprovalResult afterRejection = book.TryConsume(0, intent.TargetId, acceptedPayload, 812, out _);
		Expect(failures, marker + "-removed-and-closed", afterRejection == DamageApprovalResult.Missing && !DamageAuthoritySystem.CanApplyVanillaDamage(afterRejection));
	}
}
