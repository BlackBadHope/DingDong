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
	private void AddTab(UIElement parent, DingDongHubTab tab, float leftPercent)
	{
		DingDongHubTabButton button = new(TabLabel(tab), tab);
		button.Left.Set(4f, leftPercent);
		button.Width.Set(-8f, 0.2f);
		button.Height.Set(0f, 1f);
		button.OnLeftClick += (_, _) =>
		{
			if (selectedTab == tab)
				return;
			SoundEngine.PlaySound(SoundID.MenuTick);
			SelectTab(tab);
		};
		parent.Append(button);
		tabButtons[tab] = button;
	}

	private void Refresh(bool resetScroll, QuestJournalSnapshot questSnapshot = null)
	{
		if (contentList == null || Main.LocalPlayer == null)
			return;
		contentList.Clear();
		contentCards.Clear();
		contentList.Remove(); contentScrollbar.Remove(); bookJournal?.Remove(); spread?.Remove();
		if (selectedTab == DingDongHubTab.Quests)
		{
			BuildQuests(questSnapshot);
			Recalculate();
			return;
		}

		// Every other tab is a book spread: index on the left page, the chosen page on the right.
		spread ??= new BookSpread();
		spread.Clear(keepIndexScroll: !resetScroll);
		contentPanel.Append(spread);
		switch (selectedTab)
		{
			case DingDongHubTab.Combat:
				BuildCombat(spread);
				break;
			case DingDongHubTab.Residences:
				BuildResidences(spread);
				break;
			case DingDongHubTab.Chronicle:
				BuildChronicle(spread);
				break;
			default:
				BuildHelp(spread);
				break;
		}
		Recalculate();
		spread.Finish();
	}

	private static JournalFrame Card(DamageGuideTone tone)
	{
		JournalFrame panel = new(tone);
		panel.Width.Set(0f, 1f);
		return panel;
	}

	internal static bool SameQuestPresentation(QuestJournalSnapshot previous, QuestJournalSnapshot current) =>
		previous != null && current != null && previous.Title == current.Title &&
		previous.Subtitle == current.Subtitle && previous.Summary == current.Summary &&
		previous.Entries.SequenceEqual(current.Entries);

	private static string Text(string key, params object[] args) => Language.GetTextValue($"Mods.DingDongBiomeHomes.Hub.{key}", args);

	/// <summary>Tab captions; "Жители" and "Летопись" live in the book localization (the main file stays untouched).</summary>
	private static string TabLabel(DingDongHubTab tab) => tab switch
	{
		DingDongHubTab.Residences => BookUiText.Get("Tabs.Residents"),
		DingDongHubTab.Chronicle => BookUiText.Get("Tabs.Chronicle"),
		_ => Text($"Tabs.{tab}")
	};
}
