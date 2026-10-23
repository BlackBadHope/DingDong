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

	internal void SelectDamagePage(DamageGuidePageId page)
	{
		selectedDamagePage = page;
		if (selectedTab == DingDongHubTab.Combat && contentList != null)
			Refresh(resetScroll: true);
	}
}
