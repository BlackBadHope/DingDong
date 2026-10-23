using System;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static class AccessoryEnchantingService
{
    private static readonly long[] UpgradeCosts = { 0L, 50_000L, 150_000L, 400_000L, 1_000_000L, 3_000_000L };
    internal static long UpgradeCost(Item target) => UpgradeCostForLevel(target.GetGlobalItem<AccessoryEnchantGlobalItem>().Level);
    internal static long UpgradeCostForLevel(int level) => level >= AccessoryEnchantGlobalItem.MaxLevel ? 0L : UpgradeCosts[Math.Clamp(level, 0, AccessoryEnchantGlobalItem.MaxLevel) + 1];
    internal static long FusionCost(Item target, Item donor) => Math.Clamp(Math.Max(100_000L, Math.Max(0, target?.value ?? 0) / 10L + Math.Max(0, donor?.value ?? 0) / 5L), 100_000L, 5_000_000L);

    internal static AccessoryWorkshopQuote QuoteUpgrade(Player player, Item target)
    {
        if (player == null || target == null || target.IsAir || !target.accessory) return new(0, "accessory-only");
        if (target.stack != 1) return new(0, "single-accessory");
        var state = target.GetGlobalItem<AccessoryEnchantGlobalItem>();
        if (state.Level >= AccessoryEnchantGlobalItem.MaxLevel) return new(0, "maximum-level");
        long cost = UpgradeCost(target);
        return new(cost, player.CanAfford(cost) ? "none" : "insufficient-coins");
    }

    internal static AccessoryWorkshopQuote QuoteFusion(Player player, Item target, Item donor)
    {
        if (player == null || target == null || donor == null || ReferenceEquals(target, donor)) return new(0, "invalid-transaction");
        if (target.IsAir || donor.IsAir || !target.accessory || !donor.accessory) return new(0, "accessory-only");
        if (target.stack != 1 || donor.stack != 1) return new(0, "single-accessory");
        var state = target.GetGlobalItem<AccessoryEnchantGlobalItem>();
        var donorState = donor.GetGlobalItem<AccessoryEnchantGlobalItem>();
        long cost = FusionCost(target, donor);
        if (state.Level == 0 || state.Fused.Count >= state.Level) return new(cost, "no-unlocked-slot");
        if (!AccessoryEnchantmentCatalog.IsValid(donorState.Imprint)) return new(cost, "donor-has-no-imprint");
        if (state.Fused.Contains(donorState.Imprint)) return new(cost, "duplicate-enchantment");
        return new(cost, player.CanAfford(cost) ? "none" : "insufficient-coins");
    }

    internal static bool TryUpgrade(Player player, Item target, bool charge, out string failure)
    {
        var quote = QuoteUpgrade(player, target);
        failure = quote.Failure;
        if (!quote.Allowed && !(failure == "insufficient-coins" && !charge)) return false;
        if (charge && !player.BuyItem(quote.Cost)) { failure = "coin-transaction-failed"; return false; }
        if (!target.GetGlobalItem<AccessoryEnchantGlobalItem>().TryAdvanceLevel()) throw new InvalidOperationException("Validated accessory upgrade could not commit.");
        failure = "none";
        return true;
    }

    internal static bool TryFuse(Player player, Item target, Item donor, bool charge, out string failure)
    {
        var quote = QuoteFusion(player, target, donor);
        failure = quote.Failure;
        if (!quote.Allowed && !(failure == "insufficient-coins" && !charge)) return false;
        if (charge && !player.BuyItem(quote.Cost)) { failure = "coin-transaction-failed"; return false; }
        if (!target.GetGlobalItem<AccessoryEnchantGlobalItem>().TryAddFused(donor.GetGlobalItem<AccessoryEnchantGlobalItem>().Imprint)) throw new InvalidOperationException("Validated fusion could not commit.");
        donor.TurnToAir();
        failure = "none";
        return true;
    }
}
