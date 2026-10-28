using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Fishing.Crates;

public sealed class FishingCrateLocks : ModSystem
{
    private Hook opening, eligibility;
    [ThreadStatic] private static bool openingCrate;
    /// <summary>Probes set a fixed roll (0..1) instead of Main.rand.</summary>
    [ThreadStatic] internal static float? ForcedRoll;
    private delegate void OriginalOpen(Item item, Player player);
    private delegate void OpenHook(OriginalOpen original, Item item, Player player);
    private delegate bool OriginalEligibility(Item item);
    private delegate bool EligibilityHook(OriginalEligibility original, Item item);
    public override void Load()
    {
        MethodInfo open = typeof(ItemSlot).GetMethod("TryOpenContainer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Item), typeof(Player) }, null)
            ?? throw new InvalidOperationException("Native crate opening boundary changed.");
        opening = new Hook(open, new OpenHook((original, item, player) => {
            if (!CrateLockpickCatalog.IsLockedCrate(item.type)) { original(item, player); return; }
            TryOpen(item, player, sample => original(sample, player));
        }));
        eligibility = new Hook(typeof(ItemLoader).GetMethod(nameof(ItemLoader.CanRightClick)), new EligibilityHook((original, item) => {
            if (item != null && !item.IsAir && CrateLockpickCatalog.IsLockedCrate(item.type) && !Main.dedServ &&
                ((uint)Main.myPlayer >= Main.maxPlayers || CrateLockpickCatalog.Best(Main.LocalPlayer, out _) == null)) return false;
            return original(item);
        }));
    }

    internal enum Outcome { NoPick, Opened, Broke }

    /// <summary>
    /// One attempt with the best lockpick carried: the crate is always consumed, the
    /// pick never is; the roll decides between the native loot and a broken crate.
    /// </summary>
    internal static Outcome TryOpen(Item item, Player player, Action<Item> nativeOpen)
    {
        if (openingCrate || item == null || item.IsAir || item.stack < 1 || player == null || player.dead || player.ghost) return Outcome.NoPick;
        LockpickTier tier = CrateLockpickCatalog.Best(player, out _);
        if (tier == null) return Outcome.NoPick;
        float roll = ForcedRoll ?? Main.rand.NextFloat();
        Item sample = item.Clone(); sample.stack = 1;
        if (--item.stack == 0) item.TurnToAir();
        if (roll >= tier.ChancePercent / 100f)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Shatter);
                Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.CrateLocks.Broke", sample.Name, tier.ChancePercent), new Color(244, 161, 118));
            }
            return Outcome.Broke;
        }
        openingCrate = true;
        try { nativeOpen(sample); }
        finally { openingCrate = false; }
        return Outcome.Opened;
    }
    public override void Unload() { eligibility?.Dispose(); opening?.Dispose(); eligibility = opening = null; openingCrate = false; ForcedRoll = null; }
}

public sealed class CrateLockpickTooltip : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!CrateLockpickCatalog.IsLockedCrate(item.type)) return;
        LockpickTier best = CrateLockpickCatalog.Best(Main.LocalPlayer, out _);
        string text = best == null
            ? Language.GetTextValue("Mods.DingDongBiomeHomes.CrateLocks.Requires")
            : Language.GetTextValue("Mods.DingDongBiomeHomes.CrateLocks.Chance", Lang.GetItemNameValue(CrateLockpickCatalog.PickType(best.Id)), best.ChancePercent);
        tooltips.Add(new TooltipLine(Mod, "CrateLock", text) { OverrideColor = best != null ? new Color(151, 218, 160) : new Color(244, 161, 118) });
    }
}

/// <summary>The grand lockpick opens every locked chest in the world without a key.</summary>
public sealed class GrandLockpickChestUnlock : GlobalTile
{
    public override void RightClick(int i, int j, int type)
    {
        if (Main.dedServ || type is not (TileID.Containers or TileID.Containers2) || !Chest.IsLocked(i, j)) return;
        Player player = Main.LocalPlayer;
        LockpickTier best = CrateLockpickCatalog.Best(player, out _);
        if (best == null || !best.OpensLockedChests) return;
        int x = i, y = j;
        Tile tile = Main.tile[i, j];
        if (tile.TileFrameX % 36 != 0) x--;
        if (tile.TileFrameY != 0) y--;
        if (!Chest.Unlock(x, y)) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, x, y);
        Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.CrateLocks.Unlocked"), new Color(151, 218, 160));
        // vanilla then opens the (now unlocked) chest as usual
    }
}
