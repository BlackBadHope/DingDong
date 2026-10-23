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
	internal static bool IsPacketKind(byte kind) => kind is >= (byte)DamagePacketKind.HitIntent and <= (byte)DamagePacketKind.HitCommit;
	internal static void HandlePacket(BinaryReader reader, int whoAmI, DamagePacketKind kind)
	{
		try
		{
			switch (kind)
			{
				case DamagePacketKind.HitIntent when Main.netMode == NetmodeID.Server:
					HandleIntent(DamageNetworkCodec.ReadIntent(reader), whoAmI);
					break;
				case DamagePacketKind.HitCommit when Main.netMode == NetmodeID.Server:
					HandleCommit(DamageNetworkCodec.ReadCommit(reader), whoAmI);
					break;
				case DamagePacketKind.HitReceipt when Main.netMode == NetmodeID.MultiplayerClient:
					HandleReceipt(DamageNetworkCodec.ReadReceipt(reader));
					break;
				default:
					throw new InvalidDataException($"Unexpected damage packet kind {(byte)kind} for netMode={Main.netMode}.");
			}
		}
		catch (Exception exception)when (exception is EndOfStreamException or InvalidDataException or IOException or ArgumentException or InvalidOperationException)
		{
			DingDongBiomeHomes.ConsumeRemainingPacket(reader);
			PacketRejectGuard.Reject("DAMAGE", whoAmI, (byte)kind, exception.GetType().Name);
			if (Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers)
			{
				PendingApprovals.Reject(whoAmI);
				SendReceipt(whoAmI, new DamageHitReceipt(false, -1, 0, 1f, DamageReceiptReason.MalformedPacket));
			}
		}
	}

	private static void SendPreparedIntent(DamageHitIntent intent)
	{
		ClientPendingCommits[intent.TargetId] = new ClientPendingCommit(intent, (long)Main.GameUpdateCount + ClientCommitLifetimeTicks);
		SendIntent(intent);
	}

	private static void SendIntent(DamageHitIntent intent)
	{
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		DamageNetworkCodec.WriteIntent(packet, intent);
		packet.Send();
	}

	private static void SendCommit(DamageHitCommit commit)
	{
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		DamageNetworkCodec.WriteCommit(packet, commit);
		packet.Send();
	}

	private static void SendReceipt(int toClient, DamageHitReceipt receipt)
	{
		if (Main.netMode != NetmodeID.Server || toClient < 0 || toClient >= Main.maxPlayers)
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		DamageNetworkCodec.WriteReceipt(packet, receipt);
		packet.Send(toClient);
	}
}
