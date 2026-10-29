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
        if(shell!=null) {
            float width=Math.Max(360,Main.screenWidth/Main.UIScale-32);
            float height=Math.Max(300,Main.screenHeight/Main.UIScale-56);
            height=Math.Min(900,Math.Min(height,width/1.67f));
            shell.Width.Set(height*1.67f,0);shell.Height.Set(height,0);
            shell.Top.Set(12,0);
        }
        base.Recalculate();
		if (shell == null || navigation == null || contentPanel == null)
			return;
		CalculatedStyle inner = shell.GetInnerDimensions();
		DingDongHubLayout layout = DingDongHubLayout.Calculate(inner.Width, inner.Height);
		navigation.Left.Set(0f,.505f);navigation.Width.Set(0f,.49f);
        navigation.Top.Set(-58f, 0f);
		navigation.Height.Set(44f, 0f);
		tagline?.SetText(layout.Compact ? string.Empty : Text("Tagline"));
		int index = 0;
		foreach (DingDongHubTab tab in TabOrder)
		{
			if (!tabButtons.TryGetValue(tab, out DingDongHubTabButton button))
				continue;
			int columns = TabOrder.Length;
			button.Left.Set(0f, (index % columns) / (float)columns);
			button.Top.Set((index / columns) * (TabRowHeight + 6f), 0f);
			button.Width.Set(-6f, 1f / columns);
			button.Height.Set(TabRowHeight, 0f);
			index++;
		}
		float contentTop = 72f;
		contentPanel.Top.Set(contentTop, 0f);
		contentPanel.Height.Set(-contentTop - 46f, 1f);
		shell.Recalculate();
	}
}
