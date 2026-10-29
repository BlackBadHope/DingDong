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
		shell.MaxWidth.Set(1380f, 0f);
		shell.MaxHeight.Set(820f, 0f);
		shell.HAlign = 0.5f;
		shell.VAlign = 0.5f;
		shell.SetPadding(44f);shell.PaddingLeft=84;shell.PaddingRight=84;shell.PaddingBottom=52;
		Append(shell);
        BookText title = new(Text("Title"),1.6f,Ink);
		title.Left.Set(104f,0f);title.Top.Set(2f, 0f);
		title.Width.Set(-8f, .48f);
		shell.Append(title);
        // 29.10: one line that shrinks, ending before the corner flowers of the page.
        tagline = new BookText(Text("Tagline"),.90f,InkSoft) { SingleLine = true, MinScale = .66f };
		tagline.Left.Set(104f,0f);tagline.Top.Set(41f, 0f);
		tagline.Width.Set(-40f, .48f);
		shell.Append(tagline);
		JournalCloseButton close = new();
		close.Width.Set(32f, 0f);
		close.Height.Set(32f, 0f);
		close.Left.Set(8f, 1f);
		close.Top.Set(-32f, 0f);
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
		for (int i = 0; i < TabOrder.Length; i++)
			AddTab(navigation, TabOrder[i], i / (float)TabOrder.Length);
		contentPanel = new UIElement();
		contentPanel.Top.Set(ContentTop, 0f);
		contentPanel.Width.Set(0f, 1f);
		contentPanel.Height.Set(-ContentTop - 24f, 1f);
		shell.Append(contentPanel);

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
		contentScrollbar = new BookScrollbar();
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
		if (Main.LocalPlayer == null)
			return;
		NpcQuestPlayer questPlayer = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>();
		if (selectedTab == DingDongHubTab.Chronicle)
		{
			// Codex unlocks follow quest progress, saved choices and vanilla boss flags.
			if (questPlayer.Revision != lastQuestRevision || Main.GameUpdateCount - lastQuestRefreshTick >= 120UL)
			{
				lastQuestRevision = questPlayer.Revision;
				lastQuestRefreshTick = Main.GameUpdateCount;
				int signature = ChronicleSignature();
				if (signature != lastChronicleSignature)
				{
					lastChronicleSignature = signature;
					Refresh(resetScroll: false);
				}
			}
			return;
		}
		if (selectedTab != DingDongHubTab.Quests)
			return;
		if(lastEncounterRevision!=ResidentEncounterSystem.Revision){lastEncounterRevision=ResidentEncounterSystem.Revision;bookJournal?.Reload();}
		uint trackerRevision = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>().Revision;
		if (trackerRevision != lastTrackerRevision) { lastTrackerRevision = trackerRevision; bookJournal?.Reload(); }
		if (questPlayer.Revision != lastQuestRevision || Main.GameUpdateCount - lastQuestRefreshTick >= 30UL)
		{
			// Inventory/proximity can change readiness without a quest revision. Poll the model,
			// but leave the existing UI and scroll position alone unless displayed content changed.
			// A bare revision bump is not a reason to rebuild: Visit/Observe progress and inventory
			// polling touch the revision every 15 ticks (and in MP every snapshot does). Rebuild at
			// once only when something the book shows structurally changed (stage, status,
			// performer, pause, recorded choices); text-only changes (progress counters) are
			// rebuilt at most once per 30 ticks.
			QuestJournalSnapshot snapshot = QuestJournalContent.Build(Main.LocalPlayer);
			int bookSignature = BookStateSignature(questPlayer);
			lastQuestRevision = questPlayer.Revision;
			lastQuestRefreshTick = Main.GameUpdateCount;
			bool structural = bookSignature != lastBookSignature;
			bool textOnly = !structural && !SameQuestPresentation(lastQuestSnapshot, snapshot);
			if (structural || textOnly && Main.GameUpdateCount - lastQuestRebuildTick >= 30UL)
			{
				lastBookSignature = bookSignature;
				lastQuestRebuildTick = Main.GameUpdateCount;
				Refresh(resetScroll: false, snapshot);
			}
		}
	}

	/// <summary>Fields of every resident record the book lays out (not the progress counters).</summary>
	private static int BookStateSignature(NpcQuestPlayer questPlayer)
	{
		HashCode hash = new();
		foreach ((string npcKey, ResidentQuestProgress p) in questPlayer.CurrentSnapshot())
		{
			hash.Add(npcKey);
			hash.Add(p.Completed);
			hash.Add(p.Active);
			hash.Add(p.ActiveVariant);
			hash.Add(p.AssignedTo);
			hash.Add(p.Paused);
			hash.Add(p.Active && p.ActiveGoal > 0 && p.Progress >= p.ActiveGoal);
		}
		foreach (var pair in NpcQuestWorldLedger.Choices)
		{
			hash.Add(pair.Key);
			hash.Add(pair.Value);
		}
		return hash.ToHashCode();
	}

	/// <summary>Tab order on the right page: the chronicle sits next to the quests it explains.</summary>
	internal static readonly DingDongHubTab[] TabOrder = { DingDongHubTab.Quests, DingDongHubTab.Chronicle, DingDongHubTab.Combat, DingDongHubTab.Residences, DingDongHubTab.Help };

	private void UpdateTabButtons()
	{
		foreach ((DingDongHubTab tab, DingDongHubTabButton button)in tabButtons)
			button.SetSelected(tab == selectedTab);
	}
}
