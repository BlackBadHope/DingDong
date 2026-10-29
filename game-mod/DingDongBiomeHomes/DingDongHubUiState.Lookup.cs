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
	internal void SelectTab(DingDongHubTab tab)
	{
		selectedTab = tab;
		UpdateTabButtons();
		if (contentList != null)
			Refresh(resetScroll: true);
	}

	private void SelectChronicle(string id)
	{
		selectedChronicle = id;
		Refresh(resetScroll: false);
	}

	private void SelectResidence(int reviewId)
	{
		selectedResidence = reviewId;
		Refresh(resetScroll: false);
	}

	private void SelectHelp(int topic)
	{
		selectedHelp = topic;
		Refresh(resetScroll: false);
	}

	/// <summary>Opens the quest page of a resident (from the residents tab).</summary>
	internal void ShowResidentQuests(string npcKey)
	{
		bookJournal ??= new ResidentBookJournal();
		SelectTab(DingDongHubTab.Quests);
		bookJournal.SelectResident(npcKey);
	}

	internal void SelectDamagePage(DamageGuidePageId page)
	{
		selectedDamagePage = page;
		if (selectedTab == DingDongHubTab.Combat && contentList != null)
			Refresh(resetScroll: false); // keep the chapter index where the player clicked
	}
}
