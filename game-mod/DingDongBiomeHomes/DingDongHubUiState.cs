using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;
internal sealed partial class DingDongHubUiState : UIState
{
	private const float TabRowTop = 62f;
	private const float TabRowHeight = 44f;
	private const float ContentTop = 116f;
	private static readonly Color Ink = DingDongUiTheme.Text;
	private static readonly Color InkSoft = DingDongUiTheme.Muted;
	private static readonly Color InkGold = DingDongUiTheme.Gold;
	private readonly Dictionary<DingDongHubTab, DingDongHubTabButton> tabButtons = new();
	private readonly List<UIElement> contentCards = new();
	private JournalFrame shell;
	private UIElement navigation;
	private UIText tagline;
	private JournalSectionHeader contentHeader;
	private DingDongHubTab selectedTab = DingDongHubTab.Quests;
	private DamageGuidePageId selectedDamagePage = DamageGuidePageId.Overview;
	private UIElement contentPanel;
	private UIText contentTitle;
	private UIText contentSubtitle;
	private UIList contentList;
	private UIScrollbar contentScrollbar;
	private uint lastQuestRevision;
	private ulong lastQuestRefreshTick;
	private QuestJournalSnapshot lastQuestSnapshot;
	private Point lastScreenSize;
	private float lastUiScale;
	internal DingDongHubTab SelectedTab => selectedTab;
	internal DamageGuidePageId SelectedDamagePage => selectedDamagePage;
}
