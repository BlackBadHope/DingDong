using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	private static void ReceiveActionRequest(BinaryReader reader, int whoAmI)
	{
		AccessoryStationAction action = (AccessoryStationAction)reader.ReadByte();
		uint request = reader.ReadUInt32();
		uint expectedRevision = reader.ReadUInt32();
		if (!Enum.IsDefined(action) || !ValidPlayer(whoAmI))
			throw new InvalidDataException("Invalid accessory station action.");
		Player player = Main.player[whoAmI];
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.Session.AcceptRequest(request))
			return;
		bool success = false;
		string failure = "stale-station-revision";
		long paidCost = 0;
		if (station.Revision == expectedRevision)
		{
			failure = "goblin-session-required";
			if (TalkingToGoblin(player))
			{
				long cost = action == AccessoryStationAction.Upgrade ? (station.Target.IsAir || !station.Target.accessory ? 0L : AccessoryEnchantingService.UpgradeCost(station.Target)) : AccessoryEnchantingService.FusionCost(station.Target, station.Donor);
				Execute(player, action, out success, out failure);
				if (success)
				{
					paidCost = cost;
					station.AdvanceRevision();
				}
			}
		}
		SendActionResult(player, request, success, failure, paidCost, cursorChanged: false, whoAmI);
	}
	private static void ReceiveActionResult(BinaryReader reader)
	{
		int playerIndex = reader.ReadByte();
		uint request = reader.ReadUInt32();
		bool success = reader.ReadBoolean();
		string failure = reader.ReadString();
		long paidCost = reader.ReadInt64();
		uint revision = reader.ReadUInt32();
		bool cursorChanged = reader.ReadBoolean();
		Item target = ReadAccessory(reader);
		Item donor = ReadAccessory(reader);
		Item cursor = ItemIO.Receive(reader, true, true);
		if (!ValidPlayer(playerIndex) || playerIndex != Main.myPlayer || paidCost < 0 || paidCost > 5_000_000L || (!success && paidCost != 0))
			throw new InvalidDataException("Invalid accessory station action result.");
		Player player = Main.player[playerIndex];
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.Session.Complete(request))
			return;
		// BuyItem consumes an already-calculated price through the same wallet /
		// bank algorithm as the server. Correlation makes the mirror once-only.
		if (paidCost != 0 && !player.BuyItem(paidCost))
		{
			AbortPending("payment confirmation failed");
			return;
		}
		ApplySlots(station, target, donor, revision);
		if (cursorChanged)
		{
			player.inventory[PlayerItemSlotID.InventoryMouseItem] = cursor;
			Main.mouseItem = cursor.Clone();
		}
		AccessoryEnchantingUiSystem.NotifyResult(success, failure);
	}
	private static void SendSnapshot(Player player, int toClient, uint request)
	{
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(SlotSyncPacket);
		packet.Write((byte)player.whoAmI);
		packet.Write(request);
		packet.Write(station.Revision);
		ItemIO.Send(station.Target, packet, true, true);
		ItemIO.Send(station.Donor, packet, true, true);
		packet.Send(toClient);
	}
	private static void SendActionResult(Player player, uint request, bool success, string failure, long paidCost, bool cursorChanged, int toClient)
	{
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(ActionResultPacket);
		packet.Write((byte)player.whoAmI);
		packet.Write(request);
		packet.Write(success);
		packet.Write(failure ?? "none");
		packet.Write(paidCost);
		packet.Write(station.Revision);
		packet.Write(cursorChanged);
		ItemIO.Send(station.Target, packet, true, true);
		ItemIO.Send(station.Donor, packet, true, true);
		ItemIO.Send(player.inventory[PlayerItemSlotID.InventoryMouseItem], packet, true, true);
		packet.Send(toClient);
	}
}
