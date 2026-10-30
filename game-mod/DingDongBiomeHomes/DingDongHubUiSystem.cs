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
public sealed class DingDongHubUiSystem : ModSystem
{
	private static DingDongHubUiState hubState;
	// Owner, 30.10: after switching the game to Russian the book kept English tabs and filters;
	// they are built once, so the book is rebuilt when the language changes.
	private static string hubCulture;
	public override void Load()
	{
		if (!Main.dedServ)
			hubState = new DingDongHubUiState();
	}

	private static void RebuildOnLanguageChange()
	{
		string culture = Language.ActiveCulture.Name;
		if (hubCulture == null) { hubCulture = culture; return; }
		if (hubCulture == culture) return;
		hubCulture = culture;
		hubState = new DingDongHubUiState();
	}

	public override void Unload()
	{
		CloseMenuProbe();
		hubState = null;
	}
	internal static void Open(DingDongHubTab tab = DingDongHubTab.Quests)
	{
		if (Main.dedServ || hubState == null)
			return;
		RebuildOnLanguageChange();
		hubState.SelectTab(tab);
		IngameFancyUI.OpenUIState(hubState);
	}

	/// <summary>Opens the quest book on one resident (HUD tracker click).</summary>
	internal static void OpenResident(string npcKey)
	{
		if (Main.dedServ || hubState == null)
			return;
		RebuildOnLanguageChange();
		hubState.ShowResidentQuests(npcKey);
		if (Main.InGameUI.CurrentState != hubState)
			IngameFancyUI.OpenUIState(hubState);
	}

	internal static void OpenCombat(DamageGuidePageId page)
	{
		if (Main.dedServ || hubState == null)
			return;
		RebuildOnLanguageChange();
		hubState.SelectDamagePage(page);
		hubState.SelectTab(DingDongHubTab.Combat);
		IngameFancyUI.OpenUIState(hubState);
	}

	internal static void OpenCombatForMenuProbe(DamageGuidePageId page)
	{
		if (Main.dedServ || hubState == null)
			return;
		RebuildOnLanguageChange();
		hubState.SelectDamagePage(page);
		hubState.SelectTab(DingDongHubTab.Combat);
		Main.MenuUI.SetState(hubState);
	}

	internal static void CloseMenuProbe()
	{
		if (hubState != null && Main.MenuUI.CurrentState == hubState)
			Main.MenuUI.SetState(null);
	}

	internal static void Toggle(DingDongHubTab tab)
	{
		if (Main.dedServ || hubState == null)
			return;
		if (Main.InGameUI.CurrentState == hubState)
		{
			if (hubState.SelectedTab == tab)
				IngameFancyUI.Close();
			else
				hubState.SelectTab(tab);
			return;
		}

		Open(tab);
	}

	internal static void MoveDamagePageForProbe(int delta)
	{
		if (!Main.dedServ && hubState != null)
			hubState.ChangeDamagePage(delta);
	}

	internal static DingDongHubProbeState CaptureProbeState() => hubState?.CaptureProbeState() ?? new DingDongHubProbeState(false, DingDongHubTab.Quests, DamageGuidePageId.Overview, 0, 0, string.Empty);
}
