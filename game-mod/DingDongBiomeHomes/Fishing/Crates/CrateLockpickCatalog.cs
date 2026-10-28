using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Fishing.Content;

namespace DingDongBiomeHomes.Fishing.Crates;

/// <summary>
/// Owner 28.10 (brother's request): four universal lockpicks instead of 25 themed
/// ones, one per game stage, differing only in the chance to open a fished crate.
/// A lockpick is never consumed; a failed attempt breaks the crate. Each tier is
/// crafted from one boss relic of its stage, the last one also opens every locked
/// chest in the world (biome chests of the dungeon included) without a key.
/// </summary>
internal sealed record LockpickTier(string Id, int Level, int ChancePercent, string En, string Ru, Color Color, bool OpensLockedChests, string RelicGroup, int[] Relics);

internal static class CrateLockpickCatalog
{
    internal static readonly LockpickTier[] Tiers = {
        new("Apprentice", 1, 25, "Apprentice lockpick", "Отмычка новичка", new(180,194,206), false, "DingDongRelicFirstBosses",
            new int[] { ItemID.KingSlimeMasterTrophy, ItemID.EyeofCthulhuMasterTrophy, ItemID.EaterofWorldsMasterTrophy, ItemID.BrainofCthulhuMasterTrophy }),
        new("Journeyman", 2, 50, "Journeyman lockpick", "Отмычка подмастерья", new(255,211,78), false, "DingDongRelicMechanical",
            new int[] { ItemID.DestroyerMasterTrophy, ItemID.TwinsMasterTrophy, ItemID.SkeletronPrimeMasterTrophy }),
        new("Master", 3, 75, "Master lockpick", "Мастерская отмычка", new(227,159,240), false, "",
            new int[] { ItemID.GolemMasterTrophy }),
        new("Grand", 4, 100, "Grand lockpick", "Супер-отмычка", new(118,222,199), true, "",
            new int[] { ItemID.MoonLordMasterTrophy })
    };
    private static readonly Dictionary<string, int> picks = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, LockpickTier> tierByType = new();
    private static readonly HashSet<int> lockedCrates = new();
    internal static IReadOnlyCollection<int> LockedCrates => lockedCrates;

    internal static void Register(Mod mod)
    {
        foreach (var tier in Tiers) mod.AddContent(new CrateLockpick(tier));
    }

    internal static void Initialize()
    {
        picks.Clear(); tierByType.Clear(); lockedCrates.Clear();
        foreach (var tier in Tiers)
        {
            int type = ModContent.Find<ModItem>("DingDongBiomeHomes/" + tier.Id + "CrateLockpick").Type;
            picks.Add(tier.Id, type); tierByType[type] = tier;
        }
        foreach (var pair in ContentSamples.ItemsByType)
        {
            int type = pair.Key; Item item = pair.Value;
            // Wooden crates are the free early-game tier: they open without a pick.
            if (type is ItemID.WoodenCrate or ItemID.WoodenCrateHard) continue;
            if (ItemID.Sets.IsFishingCrate[type]) lockedCrates.Add(type);
            else if (item.ModItem is GeneratedFishingItem generated && generated.Definition.Kind == FishingContentKind.Treasure) lockedCrates.Add(type);
        }
    }

    internal static int PickType(string id) => picks.TryGetValue(id, out int type) ? type : 0;
    internal static bool IsLockedCrate(int crate) => lockedCrates.Contains(crate);
    internal static bool TryGetTier(int itemType, out LockpickTier tier) => tierByType.TryGetValue(itemType, out tier);

    /// <summary>
    /// The best lockpick in the player's inventory or in a carried keyring, or null.
    /// <paramref name="slot"/> is the inventory slot of that pick, or -1 when the best
    /// pick sits in the keyring. Lockpicks are never consumed, so callers only read
    /// the tier and must not touch the item by slot.
    /// </summary>
    internal static LockpickTier Best(Player player, out int slot)
    {
        slot = -1; LockpickTier best = null;
        if (player?.inventory == null) return null;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item == null || item.IsAir || !tierByType.TryGetValue(item.type, out LockpickTier tier)) continue;
            if (best == null || tier.Level > best.Level) { best = tier; slot = i; }
        }
        // KeyringItem.IsKey routes lockpicks into the ring (store click / pickup), so a pick kept there counts too.
        var ring = global::DingDongBiomeHomes.Items.Keyring.KeyringItem.Find(player);
        if (ring != null)
            foreach (Item item in ring.Stored)
            {
                if (item == null || item.IsAir || !tierByType.TryGetValue(item.type, out LockpickTier tier)) continue;
                if (best == null || tier.Level > best.Level) { best = tier; slot = -1; }
            }
        return best;
    }

    internal static void Reset() { picks.Clear(); tierByType.Clear(); lockedCrates.Clear(); }
}
