using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Economy;

// Owner, 30.10 (money rework): selling resources is the main income, but a resident pays from
// a real purse. The Merchant, the Party Girl, the Tax Collector and the Travelling Merchant
// are rich and never run dry; every other resident holds 10 gold to 100 platinum depending on
// the size of its house. Buying from a resident fills its purse back up to that size. Purses
// refill at midnight every fourth night. The server owns the purses; clients see a copy.
public sealed class ResidentPurseSystem : ModSystem
{
	internal const byte SnapshotPacket = 120, SpendPacket = 121;
	private const long MinPurse = 100_000, MaxPurse = 100_000_000; // 10 gold .. 100 platinum
	private const int NightsPerRefill = 4;

	private static readonly Dictionary<int, long> spent = new();
	private static readonly Dictionary<int, long> clientRemaining = new();
	private static int nights;
	private static bool wasBeforeMidnight;
	private static bool dirty;

	public override void ClearWorld() { spent.Clear(); clientRemaining.Clear(); nights = 0; dirty = false; }

	internal static bool Unlimited(int npcType) =>
		npcType is NPCID.Merchant or NPCID.PartyGirl or NPCID.TaxCollector or NPCID.TravellingMerchant;

	/// <summary>Purse size from the resident's house: a small hut holds 10 gold, a castle up to 100 platinum.</summary>
	internal static long Capacity(int npcType)
	{
		long cells = 0;
		foreach (SettlementResidenceReport home in SettlementWorldState.Residences)
			if (NpcResidentRegistry.TryGetType(home.NpcKey, out int type) && type == npcType) { cells = (long)home.Area.Width * home.Area.Height; break; }
		if (cells <= 0) return 1_000_000; // a resident without an authored house: 1 platinum
		double t = Math.Clamp((Math.Log10(cells) - Math.Log10(600)) / (Math.Log10(165_000) - Math.Log10(600)), 0, 1);
		return (long)Math.Round(MinPurse * Math.Pow(MaxPurse / (double)MinPurse, t));
	}

	internal static long Remaining(int npcType)
	{
		if (Unlimited(npcType)) return long.MaxValue;
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return clientRemaining.TryGetValue(npcType, out long left) ? left : Capacity(npcType);
		return Math.Max(0, Capacity(npcType) - spent.GetValueOrDefault(npcType));
	}

	/// <summary>Coins move: positive when the resident pays the player, negative when the player pays the resident.</summary>
	internal static void Spend(int npcType, long copper)
	{
		if (Unlimited(npcType) || copper == 0) return;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			clientRemaining[npcType] = Math.Clamp(Remaining(npcType) - copper, 0, Capacity(npcType));
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write(SpendPacket); packet.Write(npcType); packet.Write(copper); packet.Send();
			return;
		}
		spent[npcType] = Math.Clamp(spent.GetValueOrDefault(npcType) + copper, 0, Capacity(npcType));
		dirty = true;
	}

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		// Midnight is 4:30 into the night (16200 ticks).
		bool beforeMidnight = !Main.dayTime && Main.time < 16200;
		if (wasBeforeMidnight && !Main.dayTime && Main.time >= 16200)
		{
			nights++;
			if (nights % NightsPerRefill == 0 && spent.Count > 0) { spent.Clear(); dirty = true; }
		}
		wasBeforeMidnight = beforeMidnight;
		if (Main.netMode == NetmodeID.Server && (dirty && Main.GameUpdateCount % 30 == 0 || Main.GameUpdateCount % 900 == 0))
		{
			dirty = false;
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write(SnapshotPacket);
			var types = new List<int>();
			foreach (NPC npc in Main.ActiveNPCs) if (npc.townNPC && !Unlimited(npc.type) && !types.Contains(npc.type)) types.Add(npc.type);
			packet.Write((short)types.Count);
			foreach (int type in types) { packet.Write(type); packet.Write(Remaining(type)); }
			packet.Send();
		}
	}

	internal static bool IsPacket(byte kind) => kind is SnapshotPacket or SpendPacket;

	internal static void HandlePacket(byte kind, BinaryReader reader, int whoAmI)
	{
		if (kind == SpendPacket)
		{
			int type = reader.ReadInt32(); long copper = reader.ReadInt64();
			if (Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers && type > 0 && type < NPCLoader.NPCCount)
				Spend(type, copper);
			return;
		}
		if (Main.netMode != NetmodeID.MultiplayerClient) return;
		clientRemaining.Clear();
		int count = reader.ReadInt16();
		for (int i = 0; i < count; i++) clientRemaining[reader.ReadInt32()] = reader.ReadInt64();
	}

	public override void SaveWorldData(TagCompound tag)
	{
		tag["PurseSpentV1"] = spent.Select(p => new TagCompound { ["t"] = NpcSaveKey(p.Key), ["c"] = p.Value }).Where(t => t.GetString("t").Length > 0).ToList();
		tag["PurseNightsV1"] = nights;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		spent.Clear();
		nights = tag.ContainsKey("PurseNightsV1") ? tag.GetInt("PurseNightsV1") : 0;
		foreach (TagCompound entry in tag.GetList<TagCompound>("PurseSpentV1"))
		{
			int type = NpcFromSaveKey(entry.GetString("t"));
			if (type > 0) spent[type] = entry.GetLong("c");
		}
	}

	private static string NpcSaveKey(int type) => type < NPCID.Count ? "Terraria/" + NPCID.Search.GetName(type) : NPCLoader.GetNPC(type)?.FullName ?? string.Empty;

	private static int NpcFromSaveKey(string key)
	{
		if (key.StartsWith("Terraria/", StringComparison.Ordinal)) return NPCID.Search.TryGetId(key["Terraria/".Length..], out int id) ? id : 0;
		return ModContent.TryFind<ModNPC>(key, out ModNPC npc) ? npc.Type : 0;
	}

	internal static string Coins(long copper) => QuestRewardBalance.FormatCoins((int)Math.Min(int.MaxValue, copper));
}

public sealed class ResidentPursePlayer : ModPlayer
{
	private static long SellValue(Item item) => (long)(item.value / 5) * Math.Max(1, item.stack);

	public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item)
	{
		if (vendor == null || ResidentPurseSystem.Unlimited(vendor.type)) return true;
		long price = SellValue(item), left = ResidentPurseSystem.Remaining(vendor.type);
		if (price <= left) return true;
		if (Player.whoAmI == Main.myPlayer)
			Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.Economy.PurseEmpty", vendor.GivenOrTypeName, ResidentPurseSystem.Coins(left), ResidentPurseSystem.Coins(price)), new Color(244, 170, 90));
		return false;
	}

	public override void PostSellItem(NPC vendor, Item[] shopInventory, Item item)
	{
		if (vendor != null && Player.whoAmI == Main.myPlayer) ResidentPurseSystem.Spend(vendor.type, SellValue(item));
	}

	public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
	{
		if (vendor == null || Player.whoAmI != Main.myPlayer) return;
		long paid = item.shopCustomPrice ?? item.value;
		ResidentPurseSystem.Spend(vendor.type, -paid);
	}
}
