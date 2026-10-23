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
	public override void Recalculate()
	{
		base.Recalculate();
		if (shell == null || navigation == null || contentPanel == null)
			return;
		CalculatedStyle inner = shell.GetInnerDimensions();
		DingDongHubLayout layout = DingDongHubLayout.Calculate(inner.Width, inner.Height);
		navigation.Top.Set(layout.NavigationTop, 0f);
		navigation.Height.Set(layout.NavigationHeight, 0f);
		tagline?.SetText(layout.Compact ? string.Empty : Text("Tagline"));
		int index = 0;
		foreach (DingDongHubTab tab in new[] { DingDongHubTab.Quests, DingDongHubTab.Combat, DingDongHubTab.Residences, DingDongHubTab.Help })
		{
			if (!tabButtons.TryGetValue(tab, out DingDongHubTabButton button))
				continue;
			int columns = layout.Columns;
			button.Left.Set(0f, (index % columns) / (float)columns);
			button.Top.Set((index / columns) * (TabRowHeight + 6f), 0f);
			button.Width.Set(-6f, 1f / columns);
			button.Height.Set(TabRowHeight, 0f);
			index++;
		}
		float contentTop = layout.ContentTop;
		contentPanel.Top.Set(contentTop, 0f);
		contentPanel.Height.Set(-contentTop - 24f, 1f);
		shell.Recalculate();
	}
}
