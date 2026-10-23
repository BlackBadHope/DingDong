using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	internal static bool ContractPass(out string failure)
	{
		byte[] packetIds = [SlotSyncPacket, ActionRequestPacket, ActionResultPacket, SlotSwapRequestPacket];
		if (packetIds.Length != packetIds.Distinct().Count() || packetIds.Any(DamageAuthoritySystem.IsPacketKind) || (byte)DingDongPacketKind.AccessoryStationSlotSync != SlotSyncPacket || (byte)DingDongPacketKind.AccessoryStationActionRequest != ActionRequestPacket || (byte)DingDongPacketKind.AccessoryStationActionResult != ActionResultPacket || (byte)DingDongPacketKind.AccessoryStationSlotSwapRequest != SlotSwapRequestPacket)
		{
			failure = "packet-namespace";
			return false;
		}

		Item original = new();
		original.SetDefaults(ItemID.Aglet);
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			ItemIO.Send(original, writer, true, true);
		stream.Position = 0;
		Item received;
		using (BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			received = ItemIO.Receive(reader, true, true);
		bool pass = received.type == original.type && received.stack == original.stack && received.accessory && stream.Position == stream.Length;
		Item stationItem = new();
		stationItem.TurnToAir();
		Item cursorItem = original.Clone();
		uint revision = 4;
		bool moved = AccessoryStationSwapTransaction.TryApply(ref stationItem, ref cursorItem, 4, ref revision, sessionValid: true, out _);
		Item replayCursor = new();
		replayCursor.TurnToAir();
		bool replayRejected = !AccessoryStationSwapTransaction.TryApply(ref stationItem, ref replayCursor, 4, ref revision, sessionValid: true, out _);
		pass &= moved && stationItem.type == original.type && cursorItem.IsAir && revision == 5 && replayRejected && replayCursor.IsAir;
		failure = pass ? string.Empty : "item-roundtrip-or-swap-authority";
		return pass;
	}
}
