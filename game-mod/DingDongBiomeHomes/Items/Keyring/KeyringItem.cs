using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using DingDongBiomeHomes.Fishing.Crates;

namespace DingDongBiomeHomes.Items.Keyring;

/// <summary>
/// Owner 28.10 (Nekit's request): one item that holds every key, lockpick and
/// chest key, in the spirit of the Banner Box. Right-click with a key on the
/// cursor stores it; right-click with an empty cursor opens the ring; keys
/// picked up while the ring is carried go straight into it; a locked chest
/// takes its key out of the ring when the player opens it.
/// </summary>
public sealed class KeyringItem : ModItem
{
	internal const int MaxEntries = 60;
	internal static readonly HashSet<int> VanillaKeys = new()
	{
		ItemID.GoldenKey, ItemID.ShadowKey, ItemID.TempleKey, ItemID.JungleKey, ItemID.CorruptionKey, ItemID.CrimsonKey,
		ItemID.HallowedKey, ItemID.FrozenKey, ItemID.DungeonDesertKey, ItemID.LightKey, ItemID.NightKey
	};
	// Not readonly: base.Clone is a MemberwiseClone, so Clone must give the copy its own list.
	internal List<Item> Stored = new();
	public override string Texture => "DingDongBiomeHomes/Assets/Items/Keyring";
	public override void SetStaticDefaults() { Item.ResearchUnlockCount = 1; }
	public override void SetDefaults()
	{
		Item.width = 24; Item.height = 24; Item.maxStack = 1; Item.rare = ItemRarityID.Green; Item.value = Item.sellPrice(silver: 50);
	}
	public override ModItem Clone(Item newEntity)
	{
		var clone = (KeyringItem)base.Clone(newEntity);
		clone.Stored = Stored.Select(i => i.Clone()).ToList();
		return clone;
	}

	internal static bool IsKey(Item item)
	{
		if (item == null || item.IsAir || item.ModItem is KeyringItem) return false; // a ring never goes into a ring
		if (VanillaKeys.Contains(item.type) || CrateLockpickCatalog.TryGetTier(item.type, out _)) return true;
		if (item.ModItem == null || !NamedLikeKey(item.ModItem.Name)) return false;
		// Modded fallback: only plain key-like items, never gear or blocks that merely carry "Key" in the name.
		return item.damage <= 0 && !item.accessory && item.headSlot < 0 && item.bodySlot < 0 && item.legSlot < 0 &&
			item.createTile < 0 && item.createWall < 0 && item.pick <= 0 && item.axe <= 0 && item.hammer <= 0 &&
			item.ammo == AmmoID.None && item.mountType < 0 && item.shoot <= ProjectileID.None;
	}

	/// <summary>"Key" as a whole PascalCase word: GoldKey, KeyOfNight, Key_2 - not Keyblade, Keystone, Keycard, KeyringItem.</summary>
	private static bool NamedLikeKey(string name)
	{
		for (int at = name.IndexOf("Key", StringComparison.Ordinal); at >= 0; at = name.IndexOf("Key", at + 1, StringComparison.Ordinal))
		{
			int next = at + 3;
			if (next >= name.Length || !char.IsLower(name[next])) return true;
		}
		return false;
	}

	/// <summary>Two stored entries can share a stack only when they are the same item with the same data.</summary>
	private static bool Stackable(Item into, Item from) =>
		into.type == from.type && into.prefix == from.prefix && ItemLoader.CanStack(into, from);

	internal int Count(int type) => Stored.Where(i => i.type == type).Sum(i => i.stack);

	/// <summary>Moves the whole stack into the ring; returns what could not fit.</summary>
	internal int Store(Item item)
	{
		if (InventoryTransactionGuard.IsBlocked(Main.LocalPlayer)) return item?.stack ?? 0;
		if (!IsKey(item)) return item?.stack ?? 0;
		int remaining = item.stack;
		foreach (Item slot in Stored.Where(i => i.stack < i.maxStack && Stackable(i, item)))
		{
			int moved = Math.Min(remaining, slot.maxStack - slot.stack);
			slot.stack += moved; remaining -= moved;
			if (remaining == 0) return 0;
		}
		while (remaining > 0 && Stored.Count < MaxEntries)
		{
			// A real clone keeps prefix, ModItem and GlobalItem data; the stack is capped per entry.
			Item entry = item.Clone(); entry.stack = Math.Min(remaining, Math.Max(1, entry.maxStack));
			Stored.Add(entry); remaining -= entry.stack;
		}
		return remaining;
	}

	/// <summary>Takes up to <paramref name="count"/> of one stored item (a clone of the newest entry plus entries that stack with it).</summary>
	internal Item Take(int type, int count)
	{
		if (InventoryTransactionGuard.IsBlocked(Main.LocalPlayer)) return new Item();
		Item result = null;
		for (int i = Stored.Count - 1; i >= 0 && (result == null || result.stack < count); i--)
		{
			Item entry = Stored[i];
			if (entry.type != type) continue;
			if (result == null) { result = entry.Clone(); result.stack = 0; count = Math.Min(count, Math.Max(1, result.maxStack)); }
			else if (!Stackable(result, entry)) continue;
			int moved = Math.Min(count - result.stack, entry.stack);
			entry.stack -= moved; result.stack += moved;
			if (entry.stack <= 0) Stored.RemoveAt(i);
		}
		if (result == null || result.stack <= 0) return new Item();
		return result;
	}

	public override bool CanRightClick() => !InventoryTransactionGuard.IsBlocked(Main.LocalPlayer);
	public override void RightClick(Player player)
	{
		if (InventoryTransactionGuard.IsBlocked(player)) return;
		if (!Main.mouseItem.IsAir)
		{
			if (!IsKey(Main.mouseItem)) { Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.NotAKey"), new Color(244, 161, 118)); return; }
			int left = Store(Main.mouseItem);
			if (left == 0) Main.mouseItem.TurnToAir(); else Main.mouseItem.stack = left;
			SoundEngine.PlaySound(SoundID.Grab);
			return;
		}
		KeyringUi.Toggle(this);
	}
	public override bool ConsumeItem(Player player) => false;

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		int kinds = Stored.Select(i => i.type).Distinct().Count();
		tooltips.Add(new TooltipLine(Mod, "KeyringCount", Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.Count", kinds, Stored.Sum(i => i.stack), MaxEntries)));
		string icons = string.Join(" ", Stored.GroupBy(i => i.type).Select(g => $"[i/s{g.Sum(i => i.stack)}:{g.Key}]"));
		if (icons.Length > 0) tooltips.Add(new TooltipLine(Mod, "KeyringIcons", icons));
	}

	public override void SaveData(TagCompound tag) => tag["Keys"] = Stored.Select(ItemIO.Save).ToList();
	public override void LoadData(TagCompound tag)
	{
		Stored.Clear();
		if (!tag.ContainsKey("Keys")) return;
		foreach (TagCompound entry in tag.GetList<TagCompound>("Keys"))
		{
			Item item = ItemIO.Load(entry);
			if (!item.IsAir) Stored.Add(item);
		}
	}
	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)Math.Min(byte.MaxValue, Stored.Count));
		foreach (Item item in Stored.Take(byte.MaxValue)) ItemIO.Send(item, writer, true);
	}
	public override void NetReceive(BinaryReader reader)
	{
		Stored.Clear();
		int count = reader.ReadByte();
		for (int i = 0; i < count; i++) Stored.Add(ItemIO.Receive(reader, true));
	}
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Leather, 2).AddIngredient(ItemID.IronBar, 3).AddTile(TileID.WorkBenches).Register();

	internal static KeyringItem Find(Player player)
	{
		if (player?.inventory == null) return null;
		foreach (Item item in player.inventory)
			if (item?.ModItem is KeyringItem ring) return ring;
		return null;
	}
}

public sealed class KeyringPickup : GlobalItem
{
	// Keys picked up while a ring is carried go straight into it.
	public override bool OnPickup(Item item, Player player)
	{
		if (InventoryTransactionGuard.IsBlocked(player)) return true;
		KeyringItem ring = KeyringItem.Find(player);
		if (ring == null || !KeyringItem.IsKey(item) || item.ModItem is KeyringItem) return true;
		int left = ring.Store(item);
		if (left == item.stack) return true;
		PopupText.NewText(PopupTextContext.RegularItemPickup, item, item.stack - left);
		SoundEngine.PlaySound(SoundID.Grab, player.position);
		if (left == 0) { item.TurnToAir(); return false; }
		item.stack = left;
		return true;
	}
}

/// <summary>A locked chest or door takes its key out of the ring when the player opens it.</summary>
public sealed class KeyringChestSupply : GlobalTile
{
	private static int KeyFor(int type, int frameX)
	{
		int column = frameX / 36;
		if (type == TileID.Containers) return column switch { 2 or 36 or 38 or 40 => ItemID.GoldenKey, // S6: locked green/pink/blue dungeon chests open with the golden key too
			 4 => ItemID.ShadowKey, 23 => ItemID.JungleKey, 24 => ItemID.CorruptionKey, 25 => ItemID.CrimsonKey, 26 => ItemID.HallowedKey, 27 => ItemID.FrozenKey, _ => 0 };
		if (type == TileID.Containers2) return column == 13 ? ItemID.DungeonDesertKey : 0;
		return 0;
	}
	public override void RightClick(int i, int j, int type)
	{
		if (Main.dedServ || type is not (TileID.Containers or TileID.Containers2) || !Chest.IsLocked(i, j)) return;
		Player player = Main.LocalPlayer;
		if (InventoryTransactionGuard.IsBlocked(player)) return;
		KeyringItem ring = KeyringItem.Find(player);
		if (ring == null) return;
		int key = KeyFor(type, Main.tile[i, j].TileFrameX);
		if (key == 0 || player.HasItem(key) || ring.Count(key) == 0) return;
		// TileLoader.RightClick runs after vanilla's locked-chest block, so the ring unlocks the chest itself
		// (same path as a vanilla key: Chest.Unlock + LockAndUnlock on clients); the next click opens it.
		int x = i, y = j;
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX % 36 != 0) x--;
		if (tile.TileFrameY % 36 != 0) y--;
		if (!Chest.Unlock(x, y)) return; // e.g. biome chest before Plantera: the key stays in the ring
		if (key != ItemID.ShadowKey) ring.Take(key, 1); // vanilla never consumes the shadow key
		if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, x, y);
	}
}
