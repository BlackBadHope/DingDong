using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	internal static bool IsPacketKind(byte kind) => kind is SlotSyncPacket or ActionRequestPacket or ActionResultPacket or SlotSwapRequestPacket;
	internal static void SendInitialSlots(Player player)
	{
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.Session.TryBegin(importing: true, (uint)Main.GameUpdateCount, out uint request))
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(SlotSyncPacket);
		packet.Write(request);
		ItemIO.Send(station.Target, packet, true, true);
		ItemIO.Send(station.Donor, packet, true, true);
		packet.Send();
	}
	internal static void RequestSlotSwap(Player player, AccessoryStationSlot slot)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.Session.TryBegin(importing: false, (uint)Main.GameUpdateCount, out uint request))
			return;
		// Publish once after acquiring the guard. Vanilla ignores its owner's
		// SyncEquipment responses, so the reply commits the cursor explicitly.
		player.inventory[PlayerItemSlotID.InventoryMouseItem] = Main.mouseItem.Clone();
		NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, PlayerItemSlotID.InventoryMouseItem, Main.mouseItem.prefix);
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(SlotSwapRequestPacket);
		packet.Write((byte)slot);
		packet.Write(request);
		packet.Write(station.Revision);
		packet.Send();
	}
	internal static void SendAllSlots(Player player, int toClient)
	{
		if (Main.netMode == NetmodeID.Server && ValidPlayer(player.whoAmI) && player.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.Ready)
			SendSnapshot(player, toClient, 0);
	}
	internal static void HandlePacket(byte kind, BinaryReader reader, int whoAmI)
	{
		try
		{
			switch (kind)
			{
				case SlotSyncPacket when Main.netMode == NetmodeID.Server: ReceiveInitialSlots(reader, whoAmI); break;
				case SlotSyncPacket when Main.netMode == NetmodeID.MultiplayerClient: ReceiveSnapshot(reader); break;
				case SlotSwapRequestPacket when Main.netMode == NetmodeID.Server: ReceiveSwapRequest(reader, whoAmI); break;
				case ActionRequestPacket when Main.netMode == NetmodeID.Server: ReceiveActionRequest(reader, whoAmI); break;
				case ActionResultPacket when Main.netMode == NetmodeID.MultiplayerClient: ReceiveActionResult(reader); break;
				default: throw new InvalidDataException($"Unexpected accessory station packet {kind} for netMode={Main.netMode}.");
			}
		}
		catch (Exception exception) when (exception is EndOfStreamException or IOException or InvalidDataException or ArgumentException)
		{
			DingDongBiomeHomes.ConsumeRemainingPacket(reader);
			PacketRejectGuard.Reject("ACCESSORY", whoAmI, kind, exception.GetType().Name);
		}
	}
	private static void ReceiveInitialSlots(BinaryReader reader, int whoAmI)
	{
		uint request = reader.ReadUInt32();
		Item target = ReadAccessory(reader);
		Item donor = ReadAccessory(reader);
		if (!AccessoryImportValidation.Plausible(target) || !AccessoryImportValidation.Plausible(donor))
			throw new InvalidDataException("Implausible imported accessory station item.");
		// SyncPlayer runs after assignment (packet 3), before world/spawn
		// packets activate the player. Gameplay still uses ValidPlayer.
		if (!AssignedPlayer(whoAmI))
			throw new InvalidDataException("Invalid accessory station player.");
		Player player = Main.player[whoAmI];
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		// Saved slots belong to the client. Import once per connection; gameplay
		// requests never supply replacement station items.
		if (!station.Session.AcceptImport(request))
			return;
		station.ImportSlots(target, donor);
		SendSnapshot(player, whoAmI, request);
	}
	private static void ReceiveSwapRequest(BinaryReader reader, int whoAmI)
	{
		AccessoryStationSlot slot = ReadSlot(reader);
		uint request = reader.ReadUInt32();
		uint expectedRevision = reader.ReadUInt32();
		if (!ValidPlayer(whoAmI))
			throw new InvalidDataException("Invalid accessory station player.");
		Player player = Main.player[whoAmI];
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.Session.AcceptRequest(request))
			return;
		ref Item cursorItem = ref player.inventory[PlayerItemSlotID.InventoryMouseItem];
		bool success = station.TrySwap(slot, ref cursorItem, expectedRevision, TalkingToGoblin(player), out string failure);
		SendActionResult(player, request, success, failure, 0L, cursorChanged: true, whoAmI);
	}
	private static Item ReadAccessory(BinaryReader reader)
	{
		Item item = ItemIO.Receive(reader, true, true);
		if (!AccessoryEnchantingStationPlayer.Accepts(item))
			throw new InvalidDataException("Invalid accessory station item.");
		return item;
	}
	private static void ReceiveSnapshot(BinaryReader reader)
	{
		int playerIndex = reader.ReadByte();
		uint request = reader.ReadUInt32();
		uint revision = reader.ReadUInt32();
		Item target = ReadAccessory(reader);
		Item donor = ReadAccessory(reader);
		bool ownImportReply = playerIndex == Main.myPlayer && request != 0;
		if (!AssignedPlayer(playerIndex) || (!ownImportReply && !ValidPlayer(playerIndex)))
			throw new InvalidDataException("Invalid accessory station player.");
		AccessoryEnchantingStationPlayer station = Main.player[playerIndex].GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (playerIndex == Main.myPlayer && ((request == 0 && !station.Session.Ready) || (request != 0 && !station.Session.Complete(request))))
			return;
		ApplySlots(station, target, donor, revision);
	}
	private static void ApplySlots(AccessoryEnchantingStationPlayer station, Item target, Item donor, uint revision)
	{
		if (!station.ApplyAuthoritative(AccessoryStationSlot.Target, target, revision) || !station.ApplyAuthoritative(AccessoryStationSlot.Donor, donor, revision))
			throw new InvalidDataException("Stale accessory station state.");
		Recipe.FindRecipes();
	}
	internal static void AbortPending(string reason)
	{
		AccessoryStationSession session = Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session;
		bool importing = session.Importing;
		session.Reset();
		if (importing)
		{
			// No inventory transaction occurred during initial slot import.
			// Keep the saved slots and the game connection; disable only station use.
			DingDongBiomeHomes.Instance?.Logger.Warn("Accessory station unavailable: " + reason + ". Reconnect before using the station.");
			AccessoryEnchantingUiSystem.NotifyResult(false, "station-sync-unavailable");
			return;
		}
		Main.statusText = "Accessory station: " + reason + ". Reconnect to restore the saved inventory.";
		DingDongBiomeHomes.Instance?.Logger.Error(Main.statusText);
		// A late reply must not overwrite subsequent inventory edits. End the
		// uncertain connection; its private station state is discarded with it.
		Netplay.Disconnect = true;
	}
}
