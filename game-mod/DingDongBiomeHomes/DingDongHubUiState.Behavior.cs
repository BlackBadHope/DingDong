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
		DingDongHubTabButton button = new(Text($"Tabs.{tab}"));
		button.Left.Set(4f, leftPercent);
		button.Width.Set(-8f, 0.25f);
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
		float previousPosition = contentList.ViewPosition;
		contentList.Clear();
		contentCards.Clear();
		contentCards.Add(contentHeader);
		switch (selectedTab)
		{
			case DingDongHubTab.Quests:
				BuildQuests(questSnapshot);
				break;
			case DingDongHubTab.Combat:
				BuildCombat();
				break;
			case DingDongHubTab.Residences:
				BuildResidences();
				break;
			default:
				BuildHelp();
				break;
		}

		// AddRange lays out the completed section once, not once per discovered residence.
		contentList.AddRange(contentCards);
		Recalculate();
		// UIList clamps immediately against its current extent, so restore only after layout.
		contentList.ViewPosition = resetScroll ? 0f : previousPosition;
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
}
