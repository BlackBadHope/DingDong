using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace DingDongBiomeHomes;

// 30.10 (owner): residences are no longer locked. Breaking what a resident's house was built
// from costs reputation with that resident (per player, −1000…+1000, start 0); quests and
// buying the broken blocks back earn it again. Numbers: Fix-R39/V03/RESIDENCE-REPUTATION-DESIGN-20261030.md.
public sealed class ResidentReputationSystem : ModSystem
{
	internal const int Min = -1000, Max = 1000, GrumbleAt = -100, QuestBanAt = -300, HostileAt = -600;
	internal const byte SnapshotPacket = 90, BuybackPacket = 91;
	private const int MaxBroken = 4000, CopperPerGold = 10000;

	internal sealed class BrokenBlock
	{
		internal string NpcKey = string.Empty, PlayerId = string.Empty;
		internal int X, Y, Type, Style, ItemType, Penalty;
		internal bool Wall;
		internal long Price => Math.Max(1, ContentSamples.ItemsByType.TryGetValue(ItemType, out Item item) && item.value > 0 ? (long)item.value * 10 : CopperPerGold);
	}

	private static readonly Dictionary<(string Player, string Npc), int> values = new();
	private static readonly List<BrokenBlock> broken = new();
	private static HashSet<int> craftingStations;
	private static bool dirty;

	// Client view: the local player's reputation per resident and the buyback summary per resident.
	private static readonly Dictionary<string, int> localValues = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, (int Count, long Price)> localBroken = new(StringComparer.Ordinal);

	public override void ClearWorld()
	{
		values.Clear(); broken.Clear(); localValues.Clear(); localBroken.Clear(); dirty = false;
	}

	internal static int Get(string playerId, string npcKey) =>
		playerId.Length > 0 && values.TryGetValue((playerId, npcKey), out int value) ? value : 0;

	/// <summary>Reputation of the local player as the client sees it (single player reads the world directly).</summary>
	internal static int Local(string npcKey) => Main.netMode == NetmodeID.MultiplayerClient
		? localValues.TryGetValue(npcKey, out int value) ? value : 0
		: Get(Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().PersistentId, npcKey);

	internal static (int Count, long Price) LocalBroken(string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return localBroken.TryGetValue(npcKey, out var summary) ? summary : (0, 0);
		var own = broken.Where(b => b.NpcKey == npcKey).ToList();
		return (own.Count, own.Sum(b => b.Price));
	}

	internal static void Add(Player player, string npcKey, int delta)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || delta == 0 || player == null) return;
		string id = player.GetModPlayer<NpcQuestPlayer>().PersistentId;
		if (id.Length == 0) return;
		int before = Get(id, npcKey), after = Math.Clamp(before + delta, Min, Max);
		values[(id, npcKey)] = after;
		dirty = true;
		if (before > HostileAt && after <= HostileAt) NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Reputation.Hostile", ResidentName(npcKey));
		else if (before > QuestBanAt && after <= QuestBanAt) NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Reputation.QuestBan", ResidentName(npcKey));
	}

	internal static string ResidentName(string npcKey) =>
		NpcResidentRegistry.TryGetType(npcKey, out int type) ? Lang.GetNPCNameValue(type) : npcKey;

	// ---- Penalties -------------------------------------------------------------------------

	/// <summary>Called on the server (or in single player) after a tile or wall of an authored house was destroyed.</summary>
	internal static void OnAuthoredCellBroken(string npcKey, int x, int y, int type, int style, bool wall)
	{
		Player player = NearestPlayer(x, y);
		if (player == null) return;
		int itemType = wall ? WallItem(type) : TileLoader.GetItemDropFromTypeAndStyle(type, style);
		int penalty = wall ? WallPenalty(itemType) : TilePenalty(type, itemType);
		if (penalty <= 0) return;
		Add(player, npcKey, -penalty);
		broken.Add(new BrokenBlock { NpcKey = npcKey, PlayerId = player.GetModPlayer<NpcQuestPlayer>().PersistentId, X = x, Y = y, Type = type, Style = style, ItemType = itemType, Penalty = penalty, Wall = wall });
		if (broken.Count > MaxBroken) broken.RemoveRange(0, broken.Count - MaxBroken);
		dirty = true;
	}

	private static Player NearestPlayer(int x, int y)
	{
		Vector2 at = new(x * 16 + 8, y * 16 + 8);
		Player best = null; float bestDistance = 40 * 16 * 40 * 16;
		foreach (Player player in Main.ActivePlayers)
		{
			if (player.dead) continue;
			float distance = Vector2.DistanceSquared(player.Center, at);
			if (distance < bestDistance) { best = player; bestDistance = distance; }
		}
		return best;
	}

	private static bool Hardmode(Item item) => item.rare >= ItemRarityID.LightRed;

	internal static int TilePenalty(int type, int itemType)
	{
		if (ResidentResidenceProtection.IsNatural(type)) return 0;
		ContentSamples.ItemsByType.TryGetValue(itemType, out Item item);
		int value = item?.value ?? 0;
		bool hardmode = item != null && Hardmode(item);
		int penalty;
		if (IsCraftingStation(type))
			return hardmode ? (value >= 100000 ? 300 : 200) : value < 500 ? 60 : value < 5000 ? 90 : value < 20000 ? 110 : 140;
		bool rare = item != null && (item.rare >= ItemRarityID.Orange || value >= 10000);
		if (rare) penalty = Math.Clamp(20 + (int)(15 * Math.Log10(Math.Max(1.0, value / 10000.0))), 20, 60);
		else if (Main.tileFrameImportant[type]) penalty = value < 100 ? 5 : value < 1000 ? 10 : 15;
		else penalty = 2;
		return hardmode ? penalty * 2 : penalty;
	}

	private static int WallPenalty(int itemType) =>
		ContentSamples.ItemsByType.TryGetValue(itemType, out Item item) && Hardmode(item) ? 2 : 1;

	private static readonly Dictionary<int, int> wallItems = new();

	private static int WallItem(int wallType)
	{
		if (wallItems.TryGetValue(wallType, out int cached)) return cached;
		int found = 0;
		for (int i = 1; i < ItemLoader.ItemCount && found == 0; i++)
			if (ContentSamples.ItemsByType.TryGetValue(i, out Item item) && item.createWall == wallType) found = i;
		return wallItems[wallType] = found;
	}

	private static bool IsCraftingStation(int tileType)
	{
		if (craftingStations == null)
		{
			craftingStations = new HashSet<int>();
			for (int i = 0; i < Recipe.numRecipes; i++)
				foreach (int tile in Main.recipe[i].requiredTile)
					if (tile >= 0) craftingStations.Add(tile);
		}
		return craftingStations.Contains(tileType);
	}

	// ---- Gains -----------------------------------------------------------------------------

	/// <summary>Quest stage handed in: first pass +40 (+80 in hardmode), repeats +25 (+50), a finished chain +150.</summary>
	internal static void OnQuestCompleted(Player player, string npcKey, int completedTotal, int chainLength)
	{
		bool repeat = chainLength > 0 && completedTotal > chainLength;
		int gain = repeat ? 25 : 40;
		if (Main.hardMode) gain *= 2;
		if (chainLength > 0 && completedTotal == chainLength) gain += 150;
		Add(player, npcKey, gain);
	}

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) return;
		// Negative standing mends by itself: +5 at every dawn.
		if (Main.dayTime && Main.time == 0)
			foreach (var key in values.Keys.ToList())
				if (values[key] < 0) { values[key] = Math.Min(0, values[key] + 5); dirty = true; }
		// Joining players get their standing within ten seconds even when nothing changed.
		if ((dirty && Main.GameUpdateCount % 30 == 0) || Main.GameUpdateCount % 600 == 0)
		{
			dirty = false;
			if (Main.netMode == NetmodeID.Server) foreach (Player player in Main.ActivePlayers) SendSnapshot(player.whoAmI);
		}
	}

	// ---- Buyback ---------------------------------------------------------------------------

	/// <summary>Client: pays for every broken block of this resident and asks the server to restore them.</summary>
	internal static bool RequestBuyback(string npcKey)
	{
		var (count, price) = LocalBroken(npcKey);
		if (count == 0 || !Main.LocalPlayer.CanAfford(price) || !Main.LocalPlayer.BuyItem(price)) return false;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write(BuybackPacket); packet.Write(npcKey); packet.Send();
		}
		else Restore(Main.LocalPlayer, npcKey);
		return true;
	}

	private static void Restore(Player payer, string npcKey)
	{
		foreach (BrokenBlock block in broken.Where(b => b.NpcKey == npcKey).ToList())
		{
			if (block.Wall)
			{
				if (Main.tile[block.X, block.Y].WallType == WallID.None) WorldGen.PlaceWall(block.X, block.Y, block.Type, true);
			}
			else if (!Main.tile[block.X, block.Y].HasTile)
			{
				TileObjectData data = TileObjectData.GetTileData(block.Type, block.Style);
				int ox = data?.Origin.X ?? 0, oy = data?.Origin.Y ?? 0;
				WorldGen.PlaceTile(block.X + ox, block.Y + oy, block.Type, true, true, -1, block.Style);
			}
			if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, block.X, block.Y, 4);
			// The refund goes to whoever broke it, so a friend can pay off your damage.
			Player breaker = Main.player.FirstOrDefault(p => p.active && p.GetModPlayer<NpcQuestPlayer>().PersistentId == block.PlayerId);
			if (breaker != null) Add(breaker, npcKey, block.Penalty);
			else { values[(block.PlayerId, npcKey)] = Math.Clamp(Get(block.PlayerId, npcKey) + block.Penalty, Min, Max); dirty = true; }
			broken.Remove(block);
		}
		dirty = true;
		NpcQuestSystem.Notify(payer, "Mods.DingDongBiomeHomes.Reputation.BoughtBack", ResidentName(npcKey));
	}

	// ---- Network ---------------------------------------------------------------------------

	internal static bool IsPacket(byte kind) => kind is SnapshotPacket or BuybackPacket;

	internal static void HandlePacket(byte kind, BinaryReader reader, int whoAmI)
	{
		if (kind == BuybackPacket && Main.netMode == NetmodeID.Server)
		{
			string npcKey = reader.ReadString();
			if (whoAmI >= 0 && whoAmI < Main.maxPlayers && Main.player[whoAmI].active && NpcTownManifest.TryGet(npcKey, out _))
				Restore(Main.player[whoAmI], npcKey);
			return;
		}
		if (kind == SnapshotPacket && Main.netMode == NetmodeID.MultiplayerClient)
		{
			localValues.Clear(); localBroken.Clear();
			int count = reader.ReadInt16();
			for (int i = 0; i < count; i++) localValues[reader.ReadString()] = reader.ReadInt16();
			count = reader.ReadInt16();
			for (int i = 0; i < count; i++) localBroken[reader.ReadString()] = (reader.ReadInt16(), reader.ReadInt64());
		}
	}

	private static void SendSnapshot(int whoAmI)
	{
		string id = Main.player[whoAmI].GetModPlayer<NpcQuestPlayer>().PersistentId;
		var mine = values.Where(v => v.Key.Player == id).ToList();
		var summary = broken.GroupBy(b => b.NpcKey).Select(g => (Key: g.Key, Count: g.Count(), Price: g.Sum(b => b.Price))).ToList();
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write(SnapshotPacket);
		packet.Write((short)mine.Count);
		foreach (var entry in mine) { packet.Write(entry.Key.Npc); packet.Write((short)entry.Value); }
		packet.Write((short)summary.Count);
		foreach (var entry in summary) { packet.Write(entry.Key); packet.Write((short)Math.Min(short.MaxValue, entry.Count)); packet.Write(entry.Price); }
		packet.Send(whoAmI);
	}

	public override void OnWorldLoad() => dirty = true;

	// ---- Persistence -----------------------------------------------------------------------

	public override void SaveWorldData(TagCompound tag)
	{
		tag["ReputationV1"] = values.Select(v => new TagCompound { ["p"] = v.Key.Player, ["n"] = v.Key.Npc, ["v"] = v.Value }).ToList();
		tag["BrokenV1"] = broken.Select(b => new TagCompound
		{
			["n"] = b.NpcKey, ["p"] = b.PlayerId, ["x"] = b.X, ["y"] = b.Y, ["t"] = b.Type, ["s"] = b.Style, ["i"] = b.ItemType, ["c"] = b.Penalty, ["w"] = b.Wall
		}).ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		values.Clear(); broken.Clear();
		foreach (TagCompound entry in tag.GetList<TagCompound>("ReputationV1"))
			values[(entry.GetString("p"), entry.GetString("n"))] = Math.Clamp(entry.GetInt("v"), Min, Max);
		foreach (TagCompound entry in tag.GetList<TagCompound>("BrokenV1"))
			broken.Add(new BrokenBlock
			{
				NpcKey = entry.GetString("n"), PlayerId = entry.GetString("p"), X = entry.GetInt("x"), Y = entry.GetInt("y"), Type = entry.GetInt("t"),
				Style = entry.GetInt("s"), ItemType = entry.GetInt("i"), Penalty = entry.GetInt("c"), Wall = entry.GetBool("w")
			});
	}
}
