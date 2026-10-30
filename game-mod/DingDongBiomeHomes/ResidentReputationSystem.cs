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
using Terraria.ObjectData;

namespace DingDongBiomeHomes;

// 30.10 (owner): residences are no longer locked. Breaking what a resident's house was built
// from costs reputation with that resident (per player, −1000…+1000, start 0); quests and
// buying the broken blocks back earn it again. Numbers: Fix-R39/V03/RESIDENCE-REPUTATION-DESIGN-20261030.md.
public sealed class ResidentReputationSystem : ModSystem
{
	internal const int Min = -1000, Max = 1000, GrumbleAt = -100, QuestBanAt = -300, HostileAt = -600, FriendlyAt = 300, DevotedAt = 700;
	private const int ListedPerResident = 30;
	internal const byte SnapshotPacket = 90, BuybackPacket = 91;
	private const int MaxBroken = 4000, CopperPerGold = 10000;

	internal sealed class BrokenBlock
	{
		internal string NpcKey = string.Empty, PlayerId = string.Empty;
		internal int Id, X, Y, Type, Style, ItemType, Penalty;
		internal bool Wall;
		internal long Price => Math.Max(1, ContentSamples.ItemsByType.TryGetValue(ItemType, out Item item) && item.value > 0 ? (long)item.value * 10 : CopperPerGold);
	}

	private static readonly Dictionary<(string Player, string Npc), int> values = new();
	private static readonly List<BrokenBlock> broken = new();
	// World quest clock of each resident's last handed-in stage: a finished chain repeats a day later.
	private static readonly Dictionary<string, long> lastCompletion = new(StringComparer.Ordinal);
	private static int nextBrokenId = 1;
	private static HashSet<int> craftingStations;
	private static bool dirty;

	// Client view: the local player's reputation per resident and the buyback summary per resident.
	private static readonly Dictionary<string, int> localValues = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, (int Count, long Price)> localBroken = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, List<(int Id, int Item, long Price)>> localEntries = new(StringComparer.Ordinal);

	public override void ClearWorld()
	{
		values.Clear(); broken.Clear(); lastCompletion.Clear(); localValues.Clear(); localBroken.Clear(); localEntries.Clear(); nextBrokenId = 1; dirty = false;
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

	/// <summary>The most recent broken blocks of a resident, newest first, for picking one to buy back.</summary>
	internal static IReadOnlyList<(int Id, int Item, long Price)> LocalEntries(string npcKey)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return localEntries.TryGetValue(npcKey, out var list) ? list : Array.Empty<(int, int, long)>();
		return Listed(npcKey);
	}

	private static List<(int Id, int Item, long Price)> Listed(string npcKey) =>
		broken.Where(b => b.NpcKey == npcKey).OrderByDescending(b => b.Id).Take(ListedPerResident).Select(b => (b.Id, b.ItemType, b.Price)).ToList();

	/// <summary>Shop price factor for a player's standing: friends pay less, a grumbling resident charges more.</summary>
	internal static float ShopFactor(int standing) =>
		standing >= DevotedAt ? .8f : standing >= FriendlyAt ? .9f : standing <= GrumbleAt ? 1.2f : 1f;

	/// <summary>Quest reward for a devoted player is a quarter higher.</summary>
	internal static int ScaleReward(string playerId, string npcKey, int reward) =>
		Get(playerId, npcKey) >= DevotedAt ? (int)Math.Min(int.MaxValue, reward * 5L / 4) : reward;

	/// <summary>A finished chain is offered again one in-game day after its last handed-in stage.</summary>
	internal static bool RepeatReady(string npcKey) =>
		!lastCompletion.TryGetValue(npcKey, out long at) || NpcQuestWorldLedger.Clock < at || NpcQuestWorldLedger.Clock - at >= PersonalResidentQuestCatalog.TicksPerDay;

	internal static void Add(Player player, string npcKey, int delta)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || delta == 0 || player == null) return;
		string id = player.GetModPlayer<NpcQuestPlayer>().PersistentId;
		if (id.Length == 0) return;
		int before = Get(id, npcKey), after = Math.Clamp(before + delta, Min, Max);
		values[(id, npcKey)] = after;
		dirty = true;
		AnnounceThresholds(player, npcKey, before, after);
	}

	// Owner, 30.10: every threshold crossing, in either direction, is announced in chat.
	private static void AnnounceThresholds(Player player, string npcKey, int before, int after)
	{
		string name = ResidentName(npcKey), key = null;
		if (after < before)
		{
			if (before > HostileAt && after <= HostileAt) key = "Hostile";
			else if (before > QuestBanAt && after <= QuestBanAt) key = "QuestBan";
			else if (before > GrumbleAt && after <= GrumbleAt) key = "Grumble";
			else if (before >= DevotedAt && after < DevotedAt) key = "DevotedLost";
			else if (before >= FriendlyAt && after < FriendlyAt) key = "FriendlyLost";
		}
		else
		{
			if (before < DevotedAt && after >= DevotedAt) key = "Devoted";
			else if (before < FriendlyAt && after >= FriendlyAt) key = "Friendly";
			else if (before <= GrumbleAt && after > GrumbleAt) key = "GrumbleLifted";
			else if (before <= QuestBanAt && after > QuestBanAt) key = "QuestBanLifted";
			else if (before <= HostileAt && after > HostileAt) key = "HostileLifted";
		}
		if (key != null) NpcQuestSystem.Notify(player, "Mods.DingDongBiomeHomes.Reputation." + key, name, after);
	}

	// Owner, 30.10: breaking a house block shows the loss above it, like a damage number.
	private static void ShowLoss(string npcKey, int x, int y, int penalty, int total)
	{
		var position = new Vector2(x * 16 + 8, y * 16);
		var color = new Color(235, 90, 70);
		const string key = "Mods.DingDongBiomeHomes.Reputation.Loss";
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.CombatTextString, -1, -1, NetworkText.FromKey(key, ResidentName(npcKey), -penalty, total),
				(int)color.PackedValue, position.X, position.Y);
		else if (!Main.dedServ)
			CombatText.NewText(new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16), color,
				Language.GetTextValue(key, ResidentName(npcKey), -penalty, total));
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
		ShowLoss(npcKey, x, y, penalty, Get(player.GetModPlayer<NpcQuestPlayer>().PersistentId, npcKey));
		broken.Add(new BrokenBlock { Id = nextBrokenId++, NpcKey = npcKey, PlayerId = player.GetModPlayer<NpcQuestPlayer>().PersistentId, X = x, Y = y, Type = type, Style = style, ItemType = itemType, Penalty = penalty, Wall = wall });
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
		lastCompletion[npcKey] = NpcQuestWorldLedger.Clock;
		dirty = true;
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

	/// <summary>Client: pays for one broken block (<paramref name="id"/>) or all of them (-1) and asks the server to restore them.</summary>
	internal static bool RequestBuyback(string npcKey, int id = -1)
	{
		long price;
		if (id < 0) price = LocalBroken(npcKey).Price;
		else
		{
			var entry = LocalEntries(npcKey).FirstOrDefault(e => e.Id == id);
			if (entry.Id != id) return false;
			price = entry.Price;
		}
		if (price <= 0 || !Main.LocalPlayer.CanAfford(price) || !Main.LocalPlayer.BuyItem(price)) return false;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write(BuybackPacket); packet.Write(npcKey); packet.Write(id); packet.Send();
		}
		else Restore(Main.LocalPlayer, npcKey, id);
		return true;
	}

	private static void Restore(Player payer, string npcKey, int id)
	{
		foreach (BrokenBlock block in broken.Where(b => b.NpcKey == npcKey && (id < 0 || b.Id == id)).ToList())
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
			int id = reader.ReadInt32();
			if (whoAmI >= 0 && whoAmI < Main.maxPlayers && Main.player[whoAmI].active && NpcTownManifest.TryGet(npcKey, out _))
				Restore(Main.player[whoAmI], npcKey, id);
			return;
		}
		if (kind == SnapshotPacket && Main.netMode == NetmodeID.MultiplayerClient)
		{
			localValues.Clear(); localBroken.Clear(); localEntries.Clear();
			int count = reader.ReadInt16();
			for (int i = 0; i < count; i++) localValues[reader.ReadString()] = reader.ReadInt16();
			count = reader.ReadInt16();
			for (int i = 0; i < count; i++)
			{
				string npcKey = reader.ReadString();
				localBroken[npcKey] = (reader.ReadInt16(), reader.ReadInt64());
				int listed = reader.ReadByte();
				var list = new List<(int, int, long)>(listed);
				for (int j = 0; j < listed; j++) list.Add((reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt64()));
				localEntries[npcKey] = list;
			}
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
		foreach (var entry in summary)
		{
			packet.Write(entry.Key); packet.Write((short)Math.Min(short.MaxValue, entry.Count)); packet.Write(entry.Price);
			var listed = Listed(entry.Key);
			packet.Write((byte)listed.Count);
			foreach (var (blockId, item, price) in listed) { packet.Write(blockId); packet.Write(item); packet.Write(price); }
		}
		packet.Send(whoAmI);
	}

	public override void OnWorldLoad() => dirty = true;

	// ---- Persistence -----------------------------------------------------------------------

	public override void SaveWorldData(TagCompound tag)
	{
		tag["ReputationV1"] = values.Select(v => new TagCompound { ["p"] = v.Key.Player, ["n"] = v.Key.Npc, ["v"] = v.Value }).ToList();
		tag["BrokenV1"] = broken.Select(b => new TagCompound
		{
			["id"] = b.Id, ["n"] = b.NpcKey, ["p"] = b.PlayerId, ["x"] = b.X, ["y"] = b.Y, ["t"] = b.Type, ["s"] = b.Style, ["i"] = b.ItemType, ["c"] = b.Penalty, ["w"] = b.Wall
		}).ToList();
		tag["RepeatClockV1"] = lastCompletion.Select(v => new TagCompound { ["n"] = v.Key, ["c"] = v.Value }).ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		values.Clear(); broken.Clear(); lastCompletion.Clear();
		foreach (TagCompound entry in tag.GetList<TagCompound>("ReputationV1"))
			values[(entry.GetString("p"), entry.GetString("n"))] = Math.Clamp(entry.GetInt("v"), Min, Max);
		foreach (TagCompound entry in tag.GetList<TagCompound>("BrokenV1"))
			broken.Add(new BrokenBlock
			{
				NpcKey = entry.GetString("n"), PlayerId = entry.GetString("p"), X = entry.GetInt("x"), Y = entry.GetInt("y"), Type = entry.GetInt("t"),
				Id = entry.ContainsKey("id") ? entry.GetInt("id") : 0,
				Style = entry.GetInt("s"), ItemType = entry.GetInt("i"), Penalty = entry.GetInt("c"), Wall = entry.GetBool("w")
			});
		// S23 saves had no ids: number those after the highest id kept.
		nextBrokenId = broken.Count == 0 ? 1 : broken.Max(b => b.Id) + 1;
		foreach (BrokenBlock block in broken.Where(b => b.Id == 0)) block.Id = nextBrokenId++;
		foreach (TagCompound entry in tag.GetList<TagCompound>("RepeatClockV1"))
			lastCompletion[entry.GetString("n")] = entry.GetLong("c");
	}
}
