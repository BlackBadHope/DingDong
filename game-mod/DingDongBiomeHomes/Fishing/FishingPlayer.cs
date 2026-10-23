using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DingDongBiomeHomes.Fishing.Content;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Fishing;

public sealed class FishingPlayer : ModPlayer
{
	private string selectedRig = "rig_surface";
	private readonly HashSet<string> discoveries = new(StringComparer.Ordinal);
	public int Catches { get; private set; }
	public bool DepthSurveyActive { get; private set; }
	public override void ResetEffects() => DepthSurveyActive = false;
	internal void EnableDepthSurvey() => DepthSurveyActive = true;
	public IReadOnlyCollection<string> Discoveries => discoveries;
	public byte ActiveRigIndex
	{
		get
		{
			for (byte i = 0; i < FishingContentCatalog.Rigs.Count; i++)
				if (FishingContentCatalog.Rigs[i].Id == selectedRig && CanEquip(i)) return i;
			return 0;
		}
	}
	public FishingRigSpec ValidRig(byte index) => FishingContentCatalog.Rigs.Count == 0 ? default :
		FishingContentCatalog.Rigs[index < FishingContentCatalog.Rigs.Count && CanEquip(index) ? index : 0];
	private bool CanEquip(int index)
	{
		if (index == 0) return true;
		if (index < 0 || index >= FishingContentCatalog.Rigs.Count) return false;
		var rig = FishingContentCatalog.Rigs[index];
		int type = FishingContentCatalog.GetItemType(rig.Id);
		return FishingEnvironment.Progression >= rig.RequiredProgression && type > 0 && Player.HasItem(type);
	}
	public void SelectRig(string id)
	{
		int index = -1;
		for (int i = 0; i < FishingContentCatalog.Rigs.Count; i++)
			if (FishingContentCatalog.Rigs[i].Id == id) { index = i; break; }
		if (!CanEquip(index)) return;
		selectedRig = id;
		if (Player.whoAmI == Main.myPlayer)
		{
			var rig = FishingContentCatalog.Rigs[index];
			Main.NewText(Language.ActiveCulture.Name == "ru-RU" ? $"Оснастка: {rig.NameRu}; глубина до {rig.TargetDepthTiles} блоков" :
				$"Rig: {rig.NameEn}; depth up to {rig.TargetDepthTiles} tiles", new Color(100, 210, 235));
			FishingNetwork.SendSelection(Player.whoAmI, (byte)index);
		}
	}
	internal bool AcceptSelection(byte index)
	{
		if (!CanEquip(index)) return false;
		selectedRig = FishingContentCatalog.Rigs[index].Id;
		return true;
	}
	internal void AcceptSnapshot(byte index)
	{
		if (index < FishingContentCatalog.Rigs.Count) selectedRig = FishingContentCatalog.Rigs[index].Id;
	}
	public override void SaveData(TagCompound tag)
	{
		tag["fishingRig"] = selectedRig;
		tag["fishingDiscoveries"] = discoveries.OrderBy(x => x, StringComparer.Ordinal).ToList();
		tag["fishingCatches"] = Catches;
	}
	public override void LoadData(TagCompound tag)
	{
		selectedRig = tag.GetString("fishingRig");
		if (string.IsNullOrEmpty(selectedRig)) selectedRig = "rig_surface";
		discoveries.Clear();
		foreach (string id in tag.GetList<string>("fishingDiscoveries").Take(512))
			if (FishingContentCatalog.GetItemType(id) > 0) discoveries.Add(id);
		Catches = Math.Max(0, tag.GetInt("fishingCatches"));
	}
	public override void GetFishingLevel(Item fishingRod, Item bait, ref float fishingLevel) =>
		fishingLevel += ValidRig(ActiveRigIndex).PowerBonus;
	public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn,
		ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
	{
		// Preserve all quest catches, crates, enemies, rare gear, and other mods' results.
		if (Player.whoAmI != Main.myPlayer || attempt.inLava || attempt.inHoney || attempt.crate ||
			attempt.rare || attempt.veryrare || attempt.legendary || npcSpawn > 0 || !CommonFish(itemDrop)) return;
		FishingConditions conditions = FishingEnvironment.At(Player, attempt);
		if (conditions.WaterColumnTiles < 3 || Main.rand.Next(100) >= (string.IsNullOrEmpty(conditions.EventId) ? 45 : 65)) return;
		var pick = FishingPolicy.Pick(FishingContentCatalog.Rewards, conditions, Main.rand.Next());
		if (pick is not { } reward) return;
		int type = FishingContentCatalog.GetItemType(reward.ItemId);
		if (type <= 0) return;
		itemDrop = type;
		sonar.Text = Lang.GetItemNameValue(type);
		sonar.Color = new Color(110, 215, 235);
	}
	private static bool CommonFish(int type) => type is ItemID.Bass or ItemID.AtlanticCod or ItemID.Flounder or
		ItemID.RedSnapper or ItemID.Tuna or ItemID.Shrimp or ItemID.ArmoredCavefish or ItemID.SpecularFish or
		ItemID.DoubleCod or ItemID.NeonTetra or ItemID.Ebonkoi or ItemID.Hemopiranha or ItemID.CrimsonTigerfish or ItemID.VariegatedLardfish;
	public override void ModifyCaughtFish(Item fish)
	{
		// All rules for an item share a canonical stack range; bobbers may resolve in any order.
		foreach (var reward in FishingContentCatalog.Rewards)
		{
			if (FishingContentCatalog.GetItemType(reward.ItemId) != fish.type) continue;
			fish.stack = Math.Clamp(Main.rand.Next(Math.Max(1, reward.MinimumStack), Math.Max(1, reward.MaximumStack) + 1), 1, fish.maxStack);
			Catches = Catches == int.MaxValue ? Catches : Catches + 1;
			if (discoveries.Add(reward.ItemId))
				Main.NewText((Language.ActiveCulture.Name == "ru-RU" ? "Новая находка: " : "New discovery: ") + fish.Name, new Color(125, 220, 225));
			break;
		}
	}
	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => FishingNetwork.Sync(Player.whoAmI, ActiveRigIndex, toWho, fromWho);
	public override void OnEnterWorld()
	{
		if (Player.whoAmI == Main.myPlayer) FishingNetwork.SendSelection(Player.whoAmI, ActiveRigIndex);
	}
}

internal static class FishingNetwork
{
	internal const byte Select = (byte)DingDongPacketKind.FishingRigSelect, Snapshot = (byte)DingDongPacketKind.FishingRigSnapshot;
	private const byte Version = 1;
	internal static void SendSelection(int player, byte rig)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		Send(Select, player, rig, -1, -1);
	}
	internal static void Sync(int player, byte rig, int toWho = -1, int ignore = -1)
	{
		if (Main.netMode == NetmodeID.Server) Send(Snapshot, player, rig, toWho, ignore);
	}
	private static void Send(byte kind, int player, byte rig, int toWho, int ignore)
	{
		var packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(kind); packet.Write(Version); packet.Write((byte)player); packet.Write(rig);
		packet.Send(toWho, ignore);
	}
	internal static void Handle(byte kind, BinaryReader reader, int sender)
	{
		byte version = reader.ReadByte(), player = reader.ReadByte(), rig = reader.ReadByte();
		if (version != Version || player >= Main.maxPlayers || rig >= FishingContentCatalog.Rigs.Count) return;
		if (kind == Select && Main.netMode == NetmodeID.Server && sender == player && Main.player[player].active)
		{
			var state = Main.player[player].GetModPlayer<FishingPlayer>();
			if (state.AcceptSelection(rig)) Sync(player, state.ActiveRigIndex);
		}
		else if (kind == Snapshot && Main.netMode == NetmodeID.MultiplayerClient)
			Main.player[player].GetModPlayer<FishingPlayer>().AcceptSnapshot(rig);
	}
}
