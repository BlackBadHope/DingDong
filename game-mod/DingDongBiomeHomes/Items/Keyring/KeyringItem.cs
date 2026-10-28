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
	internal readonly List<Item> Stored = new();
	public override string Texture => "DingDongBiomeHomes/Assets/Items/Keyring";
	public override void SetStaticDefaults() { Item.ResearchUnlockCount = 1; }
	public override void SetDefaults()
	{
		Item.width = 24; Item.height = 24; Item.maxStack = 1; Item.rare = ItemRarityID.Green; Item.value = Item.sellPrice(silver: 50);
	}
	public override ModItem Clone(Item newEntity)
	{
		var clone = (KeyringItem)base.Clone(newEntity);
		clone.Stored.AddRange(Stored.Select(i => i.Clone()));
		return clone;
	}

	internal static bool IsKey(Item item)
	{
		if (item == null || item.IsAir) return false;
		if (VanillaKeys.Contains(item.type) || CrateLockpickCatalog.TryGetTier(item.type, out _)) return true;
		string name = item.ModItem?.Name ?? "";
		return item.ModItem != null && (name.EndsWith("Key", StringComparison.Ordinal) || name.Contains("Key", StringComparison.Ordinal) && !name.Contains("Keyboard", StringComparison.Ordinal));
	}

	internal int Count(int type) => Stored.Where(i => i.type == type).Sum(i => i.stack);

	/// <summary>Moves the whole stack into the ring; returns what could not fit.</summary>
	internal int Store(Item item)
	{
		if (!IsKey(item)) return item?.stack ?? 0;
		int remaining = item.stack;
		foreach (Item slot in Stored.Where(i => i.type == item.type && i.stack < i.maxStack))
		{
			int moved = Math.Min(remaining, slot.maxStack - slot.stack);
			slot.stack += moved; remaining -= moved;
			if (remaining == 0) return 0;
		}
		while (remaining > 0 && Stored.Count < MaxEntries)
		{
			Item entry = new(item.type); entry.stack = Math.Min(remaining, entry.maxStack);
			Stored.Add(entry); remaining -= entry.stack;
		}
		return remaining;
	}

	internal Item Take(int type, int count)
	{
		Item result = new(type); result.stack = 0;
		for (int i = Stored.Count - 1; i >= 0 && result.stack < count; i--)
		{
			if (Stored[i].type != type) continue;
			int moved = Math.Min(count - result.stack, Stored[i].stack);
			Stored[i].stack -= moved; result.stack += moved;
			if (Stored[i].stack == 0) Stored.RemoveAt(i);
		}
		if (result.stack == 0) result.TurnToAir();
		return result;
	}

	public override bool CanRightClick() => true;
	public override void RightClick(Player player)
	{
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
		if (type == TileID.Containers) return column switch { 2 => ItemID.GoldenKey, 4 => ItemID.ShadowKey, 23 => ItemID.JungleKey, 24 => ItemID.CorruptionKey, 25 => ItemID.CrimsonKey, 26 => ItemID.HallowedKey, 27 => ItemID.FrozenKey, _ => 0 };
		if (type == TileID.Containers2) return column == 13 ? ItemID.DungeonDesertKey : 0;
		return 0;
	}
	public override void RightClick(int i, int j, int type)
	{
		if (Main.dedServ || type is not (TileID.Containers or TileID.Containers2) || !Chest.IsLocked(i, j)) return;
		Player player = Main.LocalPlayer;
		KeyringItem ring = KeyringItem.Find(player);
		if (ring == null) return;
		int key = KeyFor(type, Main.tile[i, j].TileFrameX);
		if (key == 0 || player.HasItem(key) || ring.Count(key) == 0) return;
		Item taken = ring.Take(key, 1);
		if (taken.IsAir) return;
		player.QuickSpawnItem(player.GetSource_Misc("DingDongKeyring"), taken, 1);
		// vanilla finds the key in the inventory and unlocks the chest
	}
}
