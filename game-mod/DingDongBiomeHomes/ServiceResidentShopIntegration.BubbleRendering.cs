using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	private static string FitLabel(string text, float availableTextWidth, out float textScale)
	{
		float measured = FontAssets.MouseText.Value.MeasureString(text).X;
		textScale = DialogueBubbleLayout.TextScale(measured, availableTextWidth + DialogueBubbleLayout.HorizontalPadding * 2f);
		if (measured * textScale <= availableTextWidth)
			return text;
		const string ellipsis = "…";
		if (FontAssets.MouseText.Value.MeasureString(ellipsis).X * textScale > availableTextWidth)
			return string.Empty;
		int[] textElements = StringInfo.ParseCombiningCharacters(text);
		int low = 0;
		int high = textElements.Length;
		while (low < high)
		{
			int middle = (low + high + 1) / 2;
			int prefixLength = middle < textElements.Length ? textElements[middle] : text.Length;
			string candidate = text[..prefixLength].TrimEnd() + ellipsis;
			if (FontAssets.MouseText.Value.MeasureString(candidate).X * textScale <= availableTextWidth)
				low = middle;
			else
				high = middle - 1;
		}

		int fittedLength = low < textElements.Length ? textElements[low] : text.Length;
		return text[..fittedLength].TrimEnd() + ellipsis;
	}

	private static Color BubbleAccent(DingDongDialogueBubbleAction action) => action switch
	{
		DingDongDialogueBubbleAction.Quest => DingDongUiTheme.Gold,
		DingDongDialogueBubbleAction.Escort => DingDongUiTheme.Positive,
		DingDongDialogueBubbleAction.HomeGoods => DingDongUiTheme.Information,
		_ => DingDongUiTheme.Warning
	};

	private static void DrawBubblePanel(SpriteBatch spriteBatch, Rectangle bounds, Color accent, bool hovered)
	{
		Color fill = hovered ? Color.Lerp(DingDongUiTheme.Raised, accent, 0.12f) : DingDongUiTheme.Surface;
		DingDongUiTheme.DrawPanel(spriteBatch, bounds, fill, hovered ? accent : DingDongUiTheme.Border);
		if (bounds.Width > 8 && bounds.Height > 16)
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X + 4, bounds.Y + 8, 2, bounds.Height - 16), accent);
	}
}
