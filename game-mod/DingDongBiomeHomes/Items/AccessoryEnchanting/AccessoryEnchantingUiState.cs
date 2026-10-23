using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState : UIState
{
    private UIPanel root;
    private UIList body;
    private UIScrollbar scrollbar;
    private UIElement workshop;
    private UIText title, subtitle, targetName, targetInfo, donorName, donorInfo;
    private UIText upgradeInfo, upgradeCost, upgradeReason, fusionInfo, fusionCost, fusionReason, fusedText, statusText;
    private UITextPanel<string> upgradeButton, fusionButton, guideButton;
    private AccessoryStationSlotElement targetSlot, donorSlot;
    private bool guideOpen, upgradeReady, fusionReady;
    private string armedFusion, lastLanguage;
    private long armedAt;
    private uint lastRefresh, lastVisible;
    private string actionKey;
    private float lastWidth, lastHeight;
    private static readonly Color Ink = new(229, 234, 247);
    private static readonly Color Muted = new(173, 187, 212);
    private static readonly Color Good = new(132, 235, 183);
    private static readonly Color Warning = new(255, 200, 111);
}
