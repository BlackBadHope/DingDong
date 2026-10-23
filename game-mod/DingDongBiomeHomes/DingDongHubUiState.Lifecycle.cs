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
internal sealed partial class DingDongHubUiState
{
	public override void OnInitialize()
	{
		shell = new JournalFrame(shell: true);
		shell.Width.Set(-32f, 1f);
		shell.Height.Set(-32f, 1f);
		shell.MaxWidth.Set(1060f, 0f);
		shell.MaxHeight.Set(820f, 0f);
		shell.HAlign = 0.5f;
		shell.VAlign = 0.5f;
		shell.SetPadding(22f);
		Append(shell);
		UIText title = new(Text("Title"), 1.1f)
		{
			TextColor = InkGold,
			TextOriginX = 0f,
			IgnoresMouseInteraction = true
		};
		title.Top.Set(2f, 0f);
		title.Width.Set(-56f, 1f);
		shell.Append(title);
		tagline = new UIText(Text("Tagline"), 0.72f) { TextColor = InkSoft, TextOriginX = 0f, IgnoresMouseInteraction = true };
		tagline.Top.Set(31f, 0f);
		tagline.Width.Set(-56f, 1f);
		shell.Append(tagline);
		JournalCloseButton close = new();
		close.Width.Set(40f, 0f);
		close.Height.Set(40f, 0f);
		close.Left.Set(-40f, 1f);
		close.Top.Set(0f, 0f);
		close.OnLeftClick += (_, _) =>
		{
			SoundEngine.PlaySound(SoundID.MenuClose);
			IngameFancyUI.Close();
		};
		shell.Append(close);
		navigation = new UIElement();
		navigation.Top.Set(TabRowTop, 0f);
		navigation.Width.Set(0f, 1f);
		navigation.Height.Set(TabRowHeight, 0f);
		shell.Append(navigation);
		AddTab(navigation, DingDongHubTab.Quests, 0f);
		AddTab(navigation, DingDongHubTab.Combat, 0.25f);
		AddTab(navigation, DingDongHubTab.Residences, 0.5f);
		AddTab(navigation, DingDongHubTab.Help, 0.75f);
		contentPanel = new UIElement();
		contentPanel.Top.Set(ContentTop, 0f);
		contentPanel.Width.Set(0f, 1f);
		contentPanel.Height.Set(-ContentTop - 24f, 1f);
		shell.Append(contentPanel);
		UIText footer = new(Text("ScrollHint"), 0.7f) { TextColor = InkSoft, IgnoresMouseInteraction = true };
		footer.Top.Set(-14f, 1f);
		shell.Append(footer);
		contentHeader = new JournalSectionHeader();
		contentTitle = contentHeader.Title;
		contentSubtitle = contentHeader.Subtitle;
		contentList = new UIList
		{
			ListPadding = 10f,
			ManualSortMethod = _ =>
			{
			}
		};
		contentList.Top.Set(0f, 0f);
		contentList.Width.Set(-28f, 1f);
		contentList.Height.Set(-2f, 1f);
		contentPanel.Append(contentList);
		contentList.Add(contentHeader);
		contentScrollbar = new UIScrollbar();
		contentScrollbar.Top.Set(0f, 0f);
		contentScrollbar.Left.Set(-24f, 1f);
		contentScrollbar.Width.Set(24f, 0f);
		contentScrollbar.Height.Set(-2f, 1f);
		contentScrollbar.SetView(100f, 1000f);
		contentPanel.Append(contentScrollbar);
		contentList.SetScrollbar(contentScrollbar);
		UpdateTabButtons();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Point screenSize = new(Main.screenWidth, Main.screenHeight);
		if (lastScreenSize != screenSize || lastUiScale != Main.UIScale)
		{
			lastScreenSize = screenSize;
			lastUiScale = Main.UIScale;
			Recalculate();
		}
		if (selectedTab != DingDongHubTab.Quests || Main.LocalPlayer == null)
			return;
		NpcQuestPlayer questPlayer = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>();
		if (questPlayer.Revision != lastQuestRevision || Main.GameUpdateCount - lastQuestRefreshTick >= 30UL)
		{
			// Inventory/proximity can change readiness without a quest revision. Poll the model,
			// but leave the existing UI and scroll position alone unless displayed content changed.
			QuestJournalSnapshot snapshot = QuestJournalContent.Build(Main.LocalPlayer);
			lastQuestRevision = questPlayer.Revision;
			lastQuestRefreshTick = Main.GameUpdateCount;
			if (!SameQuestPresentation(lastQuestSnapshot, snapshot))
				Refresh(resetScroll: false, snapshot);
		}
	}

	private void UpdateTabButtons()
	{
		foreach ((DingDongHubTab tab, DingDongHubTabButton button)in tabButtons)
			button.SetSelected(tab == selectedTab);
	}
}
