using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!AccessoryEnchantingUiSystem.Visible) return;
        uint now = (uint)Main.GameUpdateCount;
        if (unchecked(now - lastVisible) > 1) { armedFusion = null; lastRefresh = 0; }
        lastVisible = now;
        if (root.ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        string language = Language.ActiveCulture?.Name ?? "en-US";
        if (lastWidth != Terraria.GameInput.PlayerInput.OriginalScreenSize.X / Main.UIScale || lastHeight != Terraria.GameInput.PlayerInput.OriginalScreenSize.Y / Main.UIScale || language != lastLanguage)
        {
            lastLanguage = language; Reflow(); RebuildContent();
        }
        if (!guideOpen && (lastRefresh == 0 || unchecked(now - lastRefresh) >= 6)) Refresh();
    }

    private void Refresh()
    {
        if (guideOpen) return;
        uint now = (uint)Main.GameUpdateCount;
        lastRefresh = now;
        var player = Main.LocalPlayer;
        var station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
        var target = station.Target; var donor = station.Donor;
        bool hasTarget = target?.IsAir == false && target.accessory;
        bool hasDonor = donor?.IsAir == false && donor.accessory;
        var t = hasTarget ? target.GetGlobalItem<AccessoryEnchantGlobalItem>() : null;
        var d = hasDonor ? donor.GetGlobalItem<AccessoryEnchantGlobalItem>() : null;
        var upgrade = AccessoryEnchantingService.QuoteUpgrade(player, target);
        var fusion = AccessoryEnchantingService.QuoteFusion(player, target, donor);
        bool pending = Main.netMode == NetmodeID.MultiplayerClient && (!station.Session.Ready || station.Session.Pending);
        bool session = AccessoryEnchantingNetwork.TalkingToGoblin(player);
        upgradeReady = upgrade.Allowed && !pending && session;
        fusionReady = fusion.Allowed && !pending && session;
        actionKey = FusionKey(station);
        if (!AccessoryWorkshopFormatting.Confirmed(armedFusion, actionKey, armedAt, Environment.TickCount64) || !fusionReady) armedFusion = null;
        targetName.SetText(hasTarget ? target.Name : Word("Place the accessory to keep here", "Положи сюда аксессуар, который сохраняешь"));
        donorName.SetText(hasDonor ? donor.Name : Word("Place an accessory to sacrifice here", "Положи сюда аксессуар для расхода"));
        targetInfo.SetText(hasTarget ? Word($"Level +{t.Level}/5 · slots {t.Fused.Count}/{t.Level} used\nOriginal item properties stay.", $"Уровень +{t.Level}/5 · занято {t.Fused.Count}/{t.Level} слотов\nСвойства предмета сохраняются.") : Word("Only accessories. Click a slot to insert or retrieve an item.", "Только аксессуары. Клик по ячейке кладёт или забирает предмет."));
        donorInfo.SetText(hasDonor && AccessoryEnchantmentCatalog.IsValid(d.Imprint) ? Word("Imprint: ", "Отпечаток: ") + AccessoryEnchantmentCatalog.Get(d.Imprint).Name : Word("No imprint yet. Reforge this donor using the Goblin's ordinary reforge slot.", "Отпечатка нет. Перекуй донора в обычном слоте Гоблина."));
        upgradeInfo.SetText(!hasTarget ? Word("Each upgrade opens ONE effect slot. Level alone adds no damage or defence.", "Каждое улучшение открывает ОДИН слот эффекта. Сам уровень не даёт урона или защиты.") : t.Level >= 5 ? Word("All five slots are unlocked. Install effects using a donor.", "Все пять слотов открыты. Устанавливай эффекты с помощью донора.") : Word($"Level +{t.Level} → +{t.Level + 1}\nEffect capacity {t.Level} → {t.Level + 1}. No direct stat bonus.", $"Уровень +{t.Level} → +{t.Level + 1}\nСлотов {t.Level} → {t.Level + 1}. Прямого бонуса к статам нет."));
        fusionInfo.SetText(hasDonor && AccessoryEnchantmentCatalog.IsValid(d.Imprint) ? AccessoryEnchantmentCatalog.Get(d.Imprint).Name + "\n" + AccessoryEnchantmentCatalog.Get(d.Imprint).Effect : Word("One imprint → one free slot. Donor's base item stats, prefix, upgrades and fused effects do not transfer.", "Один отпечаток → один свободный слот. Свойства, префикс, уровни и слияния донора не переносятся."));
        upgradeCost.SetText(Word("Cost: ", "Цена: ") + Coins(upgrade.Cost));
        fusionCost.SetText(Word("Cost: ", "Цена: ") + Coins(fusion.Cost));
        upgradeButton.SetText(t?.Level >= 5 ? Word("Maximum +5", "Максимум +5") : Word("Unlock next slot", "Открыть следующий слот"));
        fusionButton.SetText(armedFusion != null ? Word("Confirm · consume donor", "Подтвердить · расход донора") : Word("Review fusion", "Подготовить слияние"));
        SetEnabled(upgradeButton, upgradeReady); SetEnabled(fusionButton, fusionReady);
        upgradeReason.SetText(pending ? Word("Waiting for server…", "Ожидание сервера…") : !session ? Failure("goblin-session-required") : upgrade.Allowed ? Word("Guaranteed. Donor is not used.", "Гарантированно. Донор не расходуется.") : Failure(upgrade.Failure));
        fusionReason.SetText(pending ? Word("Waiting for server…", "Ожидание сервера…") : !session ? Failure("goblin-session-required") : fusion.Allowed ? Word($"Slots {t.Fused.Count}/{t.Level} → {t.Fused.Count + 1}/{t.Level}. Donor is destroyed!", $"Слоты {t.Fused.Count}/{t.Level} → {t.Fused.Count + 1}/{t.Level}. Донор уничтожается!") : Failure(fusion.Failure));
        string installed = t == null || t.Fused.Count == 0 ? Word("No installed effects. Unlock a slot, then transfer a donor's imprint. The transferable imprint itself is not an equipped bonus.", "Установленных эффектов нет. Открой слот и перенеси отпечаток донора. Сам переносимый отпечаток не даёт бонус при ношении.") : string.Join("\n", t.Fused.Select(id => AccessoryEnchantmentCatalog.Get(id).Name + ": " + AccessoryEnchantmentCatalog.Get(id).Effect));
        fusedText.SetText(installed);
        if (statusUntil == 0 || unchecked(now - statusAt) >= statusUntil)
        {
            statusText.TextColor = Muted;
            statusText.SetText(Word("Hover an item for its tooltip. Slots store your items safely; click to retrieve. Scroll for details.", "Наведи на предмет для подсказки. Предметы в ячейках сохраняются; кликни, чтобы забрать. Ниже — подробности."));
        }
    }

    private static string FusionKey(AccessoryEnchantingStationPlayer station)
    {
        var t = station.Target; var d = station.Donor;
        if (t == null || d == null || t.IsAir || d.IsAir) return string.Empty;
        var ts = t.GetGlobalItem<AccessoryEnchantGlobalItem>(); var ds = d.GetGlobalItem<AccessoryEnchantGlobalItem>();
        return $"{station.Revision}|{t.type}|{t.prefix}|{t.stack}|{t.value}|{ts.Level}|{ts.Imprint}|{string.Join(",", ts.Fused)}|{d.type}|{d.prefix}|{d.stack}|{d.value}|{ds.Level}|{ds.Imprint}|{string.Join(",", ds.Fused)}";
    }
}
