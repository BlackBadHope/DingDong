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
	public override void PostSetupContent()
	{
		DamageCoverageSnapshot coverage = DamageRuntimeClassifier.AuditLoadedContent();
		Mod.Logger.Info(coverage.ToMarker());
	}

	public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
	{
		if (Main.netMode != NetmodeID.Server || messageType != MessageID.DamageNPC || !Enabled || !StrictValidation)
		{
			return false;
		}
		bool validSender = playerNumber >= 0 && playerNumber < Main.maxPlayers;
		if (!validSender || !reader.BaseStream.CanSeek)
		{
			DingDongBiomeHomes.ConsumeRemainingPacket(reader);
			if (validSender)
				SendReceipt(playerNumber, new DamageHitReceipt(false, -1, 0, 1f, DamageReceiptReason.MalformedPacket));
			rejectedVanillaPackets++;
			return true;
		}

		long position = reader.BaseStream.Position;
		int targetId = -1;
		DamageStrikePayload payload = default;
		bool payloadValid = false;
		try
		{
			targetId = reader.ReadInt16();
			payload = DamageNetworkCodec.ReadStrikePayload(reader);
			payloadValid = payload.IsValid;
		}
		catch (Exception exception)when (exception is EndOfStreamException or IOException or FormatException or ArgumentException)
		{
			payloadValid = false;
		}
		finally
		{
			reader.BaseStream.Position = position;
		}

		payloadValid = payloadValid && targetId >= 0 && targetId < Main.maxNPCs && Main.npc[targetId].active;
		if (!payloadValid)
			PendingApprovals.Reject(playerNumber);
		DamagePreparedHit prepared = null;
		DamageApprovalResult approvalResult = payloadValid ? PendingApprovals.TryConsume(playerNumber, targetId, payload, (long)Main.GameUpdateCount, out prepared) : DamageApprovalResult.PayloadMismatch;
		if (CanApplyVanillaDamage(approvalResult))
		{
			CommitServerLedger(playerNumber, prepared.TentativeLedger);
			LatestClientTick[playerNumber] = Math.Max(prepared.Intent.ClientTick, LatestClientTick.GetValueOrDefault(playerNumber));
			SendReceipt(playerNumber, new DamageHitReceipt(true, targetId, prepared.Intent.AttackInstanceId, prepared.ServerMultiplier, DamageReceiptReason.Accepted));
			return false;
		}

		CorrectClientNpc(playerNumber, targetId);
		DamageReceiptReason rejectionReason = approvalResult switch
		{
			DamageApprovalResult.MissingCommit => DamageReceiptReason.MissingCommit,
			DamageApprovalResult.PayloadMismatch => DamageReceiptReason.PayloadMismatch,
			DamageApprovalResult.IdentityMismatch => DamageReceiptReason.CommitMismatch,
			_ => DamageReceiptReason.MissingApproval
		};
		SendReceipt(playerNumber, new DamageHitReceipt(false, targetId, 0, 1f, rejectionReason));
		rejectedVanillaPackets++;
		if (rejectedVanillaPackets <= 8 || rejectedVanillaPackets % 100 == 0)
			Mod.Logger.Warn($"DINGDONG_DAMAGE_REJECT|sender={playerNumber}|target={targetId}|reason={rejectionReason}|rejectedTotal={rejectedVanillaPackets}");
		return true;
	}

	private static void CorrectClientNpc(int playerNumber, int targetId)
	{
		if (targetId >= 0 && targetId < Main.maxNPCs && Main.npc[targetId].active)
			NetMessage.SendData(MessageID.SyncNPC, playerNumber, -1, null, targetId);
	}
}
