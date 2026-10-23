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
internal sealed class DingDongHubTabButton : UIElement
{
	private readonly UIText label;
	private bool selected;
	internal DingDongHubTabButton(string text)
	{
		SetPadding(8f);
		label = new UIText(text, 0.86f)
		{
			IsWrapped = true,
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextOriginX = 0.5f,
			TextOriginY = 0.5f,
			WrappedTextBottomPadding = 0f,
			TextColor = DingDongUiTheme.Muted,
			IgnoresMouseInteraction = true
		};
		label.Width.Set(0f, 1f);
		label.Height.Set(0f, 1f);
		Append(label);
	}

	internal void SetSelected(bool value) => selected = value;
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		bool lit = selected || IsMouseHovering;
		Rectangle area = GetDimensions().ToRectangle();
		DingDongUiTheme.DrawPanel(spriteBatch, area, lit ? DingDongUiTheme.Raised : DingDongUiTheme.Shell, selected ? DingDongUiTheme.Gold : DingDongUiTheme.Border);
		if (selected)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 10, area.Bottom - 3, area.Width - 20, 2), DingDongUiTheme.Gold);
		label.TextColor = selected ? DingDongUiTheme.Gold : (lit ? DingDongUiTheme.Text : DingDongUiTheme.Muted);
	}
}
