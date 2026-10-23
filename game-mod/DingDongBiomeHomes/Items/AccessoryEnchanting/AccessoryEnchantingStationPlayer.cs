using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed class AccessoryEnchantingStationPlayer : ModPlayer
{
	private const string TargetKey = "dingDongGoblinTarget";
	private const string DonorKey = "dingDongGoblinDonor";
	private readonly Item[] stationItems = [NewAirItem(), NewAirItem()];
	private uint revision;
	internal AccessoryStationSession Session { get; } = new();
	internal ref Item Slot(AccessoryStationSlot slot) => ref stationItems[(int)slot];
	internal Item Target => stationItems[(int)AccessoryStationSlot.Target];
	internal Item Donor => stationItems[(int)AccessoryStationSlot.Donor];
	internal uint Revision => revision;

	public override void Initialize()
	{
		stationItems[(int)AccessoryStationSlot.Target] = NewAirItem();
		stationItems[(int)AccessoryStationSlot.Donor] = NewAirItem();
		revision = 0;
		Session.Reset();
	}

	public override void SaveData(TagCompound tag)
	{
		if (!Target.IsAir)
			tag[TargetKey] = ItemIO.Save(Target);
		if (!Donor.IsAir)
			tag[DonorKey] = ItemIO.Save(Donor);
	}

	public override void LoadData(TagCompound tag)
	{
		stationItems[(int)AccessoryStationSlot.Target] = LoadAccessory(tag, TargetKey);
		stationItems[(int)AccessoryStationSlot.Donor] = LoadAccessory(tag, DonorKey);
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		if (Main.netMode == NetmodeID.Server)
			AccessoryEnchantingNetwork.SendAllSlots(Player, toWho);
		else if (Main.netMode == NetmodeID.MultiplayerClient && newPlayer && Player.whoAmI == Main.myPlayer)
			AccessoryEnchantingNetwork.SendInitialSlots(Player);
	}

	internal void ImportSlots(Item target, Item donor)
	{
		stationItems[(int)AccessoryStationSlot.Target] = target;
		stationItems[(int)AccessoryStationSlot.Donor] = donor;
		revision = 1;
	}

	internal void AdvanceRevision() => revision++;

	public override bool PreItemCheck() => Player.whoAmI != Main.myPlayer || Main.netMode != NetmodeID.MultiplayerClient || !Session.InventoryLocked;

	internal static bool Accepts(Item item) => item == null || item.IsAir || item.accessory;
	internal bool TrySwap(AccessoryStationSlot slot, ref Item cursorItem, uint expectedRevision, bool sessionValid, out string failure)
	{
		ref Item stationItem = ref Slot(slot);
		return AccessoryStationSwapTransaction.TryApply(ref stationItem, ref cursorItem, expectedRevision, ref revision, sessionValid, out failure);
	}

	internal bool ApplyAuthoritative(AccessoryStationSlot slot, Item item, uint authoritativeRevision)
	{
		if (authoritativeRevision < revision || !Accepts(item))
			return false;
		Slot(slot) = item;
		revision = authoritativeRevision;
		return true;
	}
	private static Item LoadAccessory(TagCompound tag, string key)
	{
		if (!tag.ContainsKey(key))
			return NewAirItem();
		Item item = ItemIO.Load(tag.GetCompound(key));
		return Accepts(item) ? item : NewAirItem();
	}

	private static Item NewAirItem()
	{
		Item item = new();
		item.TurnToAir();
		return item;
	}
}
