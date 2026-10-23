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
	private static void HandleIntent(DamageHitIntent intent, int sender)
	{
		DamageReceiptReason failure = ValidateAndPrepare(intent, sender, out float serverMultiplier);
		if (failure != DamageReceiptReason.Accepted)
			SendReceipt(sender, new DamageHitReceipt(false, intent.TargetId, intent.AttackInstanceId, serverMultiplier, failure));
	}

	private static void HandleCommit(DamageHitCommit commit, int sender)
	{
		DamageReceiptReason failure = ValidateCommit(commit, sender);
		if (failure != DamageReceiptReason.Accepted)
			SendReceipt(sender, new DamageHitReceipt(false, commit.TargetId, commit.AttackInstanceId, 1f, failure));
	}

	private static void HandleReceipt(DamageHitReceipt receipt)
	{
		if (receipt.Accepted)
			acceptedReceipts++;
		else
		{
			rejectedReceipts++;
			ClientLedger.Reset();
		}
	}
}
