using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    private bool CanSubmit() => AccessoryEnchantingUiSystem.Visible && AccessoryEnchantingNetwork.TalkingToGoblin(Main.LocalPlayer) && (Main.netMode != NetmodeID.MultiplayerClient || (Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.Ready && !Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.Pending));
    private void UpgradeClicked(UIMouseEvent evt, UIElement element)
    {
        if (!CanSubmit()) return;
        var station = Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>();
        var quote = AccessoryEnchantingService.QuoteUpgrade(Main.LocalPlayer, station.Target);
        if (!quote.Allowed) { SetStatus(Failure(quote.Failure), false); Refresh(); return; }
        armedFusion = null;
        SetStatus(Word("Applying upgrade…", "Выполняется улучшение…"), true);
        AccessoryEnchantingNetwork.RequestAction(AccessoryStationAction.Upgrade);
    }
    private void FusionClicked(UIMouseEvent evt, UIElement element)
    {
        if (!CanSubmit()) return;
        var station = Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>();
        var quote = AccessoryEnchantingService.QuoteFusion(Main.LocalPlayer, station.Target, station.Donor);
        if (!quote.Allowed) { armedFusion = null; SetStatus(Failure(quote.Failure), false); Refresh(); return; }
        string current = FusionKey(station); long now = System.Environment.TickCount64;
        if (!AccessoryWorkshopFormatting.Confirmed(armedFusion, current, armedAt, now))
        {
            armedFusion = current; armedAt = now;
            SetStatus(Word($"Confirm again within 10 seconds: {station.Donor.Name} will be DESTROYED. Cost: {Coins(quote.Cost)}.", $"Подтверди ещё раз за 10 секунд: {station.Donor.Name} будет УНИЧТОЖЕН. Цена: {Coins(quote.Cost)}."), false);
            Refresh(); return;
        }
        armedFusion = null;
        SetStatus(Word("Applying fusion…", "Выполняется слияние…"), true);
        AccessoryEnchantingNetwork.RequestAction(AccessoryStationAction.Fuse);
    }
}
