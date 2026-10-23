using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	private static DamageReceiptReason ValidateCommit(DamageHitCommit commit, int sender)
	{
		if (!Enabled || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active)
			return DamageReceiptReason.InvalidSender;
		if (!commit.Payload.IsValid || !DamagePlausibility.Plausible(commit))
		{
			PendingApprovals.Reject(sender);
			return DamageReceiptReason.PayloadMismatch;
		}

		DamageApprovalResult result = PendingApprovals.TryCommit(sender, commit, (long)Main.GameUpdateCount);
		return result switch
		{
			DamageApprovalResult.Committed => DamageReceiptReason.Accepted,
			DamageApprovalResult.DuplicateCommit => DamageReceiptReason.DuplicateCommit,
			DamageApprovalResult.IdentityMismatch => DamageReceiptReason.CommitMismatch,
			DamageApprovalResult.PayloadMismatch => DamageReceiptReason.PayloadMismatch,
			_ => DamageReceiptReason.MissingApproval
		};
	}
}
