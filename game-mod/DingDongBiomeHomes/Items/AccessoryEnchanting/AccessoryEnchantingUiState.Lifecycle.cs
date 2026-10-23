using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    public override void OnInitialize()
    {
        root = new UIPanel { BackgroundColor = new Color(18, 28, 49) * 0.98f, BorderColor = new Color(170, 133, 66) };
        root.SetPadding(14f);
        root.OnMouseOver += (_, _) => Main.LocalPlayer.mouseInterface = true;
        Append(root);
        title = Label(root, 0, 0, 420, 26, Warning, 0.98f);
        subtitle = Label(root, 0, 29, 680, 34, Muted);
        guideButton = Button(root, 0, 0, 180, 30, (_, _) => { guideOpen = !guideOpen; armedFusion = null; RebuildContent(); });
        guideButton.HAlign = 1f;
        body = new UIList { ListPadding = 0f, ManualSortMethod = _ => { } };
        body.Top.Set(70, 0); body.Width.Set(-20, 1); body.Height.Set(-146, 1);
        root.Append(body);
        scrollbar = new UIScrollbar(); scrollbar.HAlign = 1f; scrollbar.Top.Set(70, 0); scrollbar.Height.Set(-146, 1);
        root.Append(scrollbar); body.SetScrollbar(scrollbar);
        statusText = Label(root, 0, 0, 680, 40, Muted); statusText.VAlign = 1f;
        Reflow();
        RebuildContent();
    }

    private void Reflow()
    {
        float viewWidth = Terraria.GameInput.PlayerInput.OriginalScreenSize.X / Main.UIScale;
        float viewHeight = Terraria.GameInput.PlayerInput.OriginalScreenSize.Y / Main.UIScale;
        float width = viewWidth >= 1000 ? MathHelper.Clamp(viewWidth - 550, 440, 820) : viewWidth - 24;
        float height = System.Math.Min(570, viewHeight - 28);
        root.Width.Set(width, 0); root.Height.Set(height, 0);
        root.Left.Set(System.Math.Max(12, viewWidth - width - 14), 0);
        root.Top.Set(System.Math.Max(14, (viewHeight - height) * 0.72f), 0);
        subtitle.Width.Set(width - 30, 0); title.Width.Set(width - 224, 0);
        statusText.Width.Set(width - 30, 0);
        lastWidth = viewWidth; lastHeight = viewHeight;
        root.Recalculate();
    }

    private void RebuildContent()
    {
        body.Clear();
        float width = root.Width.Pixels - 50;
        title.SetText(Word("Workshop", "Мастерская"));
        subtitle.SetText(Word("1. Unlock a slot.  2. Reforge a donor.  3. Transfer its imprint.", "1. Открой слот.  2. Перекуй донора.  3. Перенеси его отпечаток."));
        guideButton.SetText(guideOpen ? Word("Back to workshop", "К улучшению") : Word("Effects & rules (14)", "Эффекты и правила (14)"));
        if (guideOpen) { BuildGuide(width); return; }
        bool narrow = width < 600;
        float half = narrow ? width : (width - 12) / 2;
        float right = narrow ? 0 : half + 12;
        float donorY = narrow ? 172 : 0;
        float upgradeY = narrow ? 344 : 172;
        float fusionY = narrow ? 576 : 172;
        float effectsY = narrow ? 808 : 404;
        workshop = new UIElement(); workshop.Width.Set(width, 0); workshop.Height.Set(effectsY + 184, 0);
        body.Add(workshop);
        var targetCard = Card(workshop, 0, 0, half, 160);
        var donorCard = Card(workshop, right, donorY, half, 160);
        Label(targetCard, 0, 0, half - 20, 23, Warning).SetText(Word("KEEP · target accessory", "СОХРАНЯЕМ · аксессуар"));
        Label(donorCard, 0, 0, half - 20, 23, Warning).SetText(Word("CONSUME · donor accessory", "РАСХОДУЕМ · донор"));
        targetSlot = new AccessoryStationSlotElement(AccessoryStationSlot.Target, SetStatus);
        donorSlot = new AccessoryStationSlotElement(AccessoryStationSlot.Donor, SetStatus);
        foreach (var pair in new[] { (Slot: targetSlot, Parent: targetCard), (Slot: donorSlot, Parent: donorCard) }) {
            pair.Slot.Top.Set(28, 0); pair.Slot.Width.Set(54, 0); pair.Slot.Height.Set(54, 0); pair.Parent.Append(pair.Slot);
        }
        targetName = Label(targetCard, 62, 28, half - 82, 52, Ink);
        donorName = Label(donorCard, 62, 28, half - 82, 52, Ink);
        targetInfo = Label(targetCard, 0, 90, half - 20, 54, Muted);
        donorInfo = Label(donorCard, 0, 90, half - 20, 54, Muted);
        var upgrade = Card(workshop, 0, upgradeY, half, 220);
        var fusion = Card(workshop, right, fusionY, half, 220);
        Label(upgrade, 0, 0, half - 20, 24, Warning, 0.88f).SetText(Word("1 · Unlock a slot", "1 · Открыть слот"));
        Label(fusion, 0, 0, half - 20, 24, Warning, 0.88f).SetText(Word("2 · Transfer an effect", "2 · Перенести эффект"));
        upgradeInfo = Label(upgrade, 0, 30, half - 20, 74, Ink);
        fusionInfo = Label(fusion, 0, 30, half - 20, 74, Ink);
        upgradeCost = Label(upgrade, 0, 108, half - 20, 22, Warning);
        fusionCost = Label(fusion, 0, 108, half - 20, 22, Warning);
        upgradeButton = Button(upgrade, 0, 138, half - 20, 34, UpgradeClicked);
        fusionButton = Button(fusion, 0, 138, half - 20, 34, FusionClicked);
        upgradeReason = Label(upgrade, 0, 178, half - 20, 34, Muted, 0.68f);
        fusionReason = Label(fusion, 0, 178, half - 20, 34, Warning, 0.68f);
        var effects = Card(workshop, 0, effectsY, width, 184);
        Label(effects, 0, 0, width - 20, 22, Good).SetText(Word("Already installed · active while equipped", "Уже установлено · работает при ношении"));
        fusedText = Label(effects, 0, 28, width - 20, 144, Ink);
        lastRefresh = 0;
        // ModSystem.Load initializes UI before players receive their ModPlayers.
        // Fill live item data only once this workshop is actually visible.
        if (AccessoryEnchantingUiSystem.Visible) Refresh();
    }
}
