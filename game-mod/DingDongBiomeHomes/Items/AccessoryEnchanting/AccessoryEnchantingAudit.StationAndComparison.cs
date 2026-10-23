using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingAudit
{
	private static void RunStationFlow(Player player, ref int stationFlows, List<string> failures)
	{
		Item slotItem = new();
		Item mouseItem = Accessory(ItemID.Aglet);
		Item weapon = new();
		weapon.SetDefaults(ItemID.WoodenSword);
		bool placed = AccessoryStationSlotElement.TrySwap(ref slotItem, ref mouseItem, out _) && slotItem.type == ItemID.Aglet && mouseItem.IsAir;
		bool removed = AccessoryStationSlotElement.TrySwap(ref slotItem, ref mouseItem, out _) && slotItem.IsAir && mouseItem.type == ItemID.Aglet;
		int slotBeforeReject = slotItem.type;
		int weaponBeforeReject = weapon.type;
		bool rejected = !AccessoryStationSlotElement.TrySwap(ref slotItem, ref weapon, out _) && slotItem.type == slotBeforeReject && weapon.type == weaponBeforeReject;
		if (placed && removed && rejected)
			stationFlows++;
		else
			Fail(failures, "station:ui-swap");
		AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
		station.Slot(AccessoryStationSlot.Target) = Accessory(ItemID.HermesBoots);
		station.Slot(AccessoryStationSlot.Donor) = Accessory(ItemID.Aglet);
		if (station.Target.type == ItemID.HermesBoots && station.Donor.type == ItemID.Aglet)
			stationFlows++;
		else
			Fail(failures, "station:slots");
		TagCompound tag = new();
		station.SaveData(tag);
		Player loadedPlayer = ProbePlayer();
		AccessoryEnchantingStationPlayer loadedStation = loadedPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>();
		loadedStation.LoadData(tag);
		if (loadedStation.Target.type == ItemID.HermesBoots && loadedStation.Donor.type == ItemID.Aglet)
			stationFlows++;
		else
			Fail(failures, "station:save-load");
		if (AccessoryEnchantingNetwork.ContractPass(out string networkFailure) && AccessoryEnchantingUiSystem.ShouldShowForState(false, false, true, true, true) && !AccessoryEnchantingUiSystem.ShouldShowForState(false, false, true, true, false))
			stationFlows++;
		else
			Fail(failures, $"station:network-ui:{networkFailure}");
	}

	private static Item Accessory(int type)
	{
		Item item = new();
		item.SetDefaults(type);
		if (!item.accessory)
			throw new InvalidOperationException($"Probe item {type} is not an accessory.");
		return item;
	}

	private static bool Equivalent(AccessoryEnchantGlobalItem left, AccessoryEnchantGlobalItem right) => left.Level == right.Level && left.Imprint == right.Imprint && left.Fused.SequenceEqual(right.Fused);
	private static void Fail(List<string> failures, string value)
	{
		if (failures.Count < 24)
			failures.Add(value.Replace('|', '/'));
	}
}
