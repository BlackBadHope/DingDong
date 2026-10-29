using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace DingDongBiomeHomes;

// Shared, deliberately quiet surfaces. Pixel borders stay crisp at Terraria's UI scale.
internal static class DingDongUiTheme
{
	internal static readonly Color Text = new(57, 36, 19);
	internal static readonly Color Muted = new(105, 78, 43);
	internal static readonly Color Gold = new(120, 78, 24);
	internal static readonly Color Positive = new(62, 110, 37);
	internal static readonly Color Information = new(48, 92, 105);
	internal static readonly Color Warning = new(137, 67, 29);
	internal static readonly Color Shell = new(225, 201, 151);
	internal static readonly Color Surface = new(233, 211, 168);
	internal static readonly Color Raised = new(246, 226, 178);
	internal static readonly Color Border = new(151, 120, 72);

	internal static Color Accent(DamageGuideTone tone) => tone switch
	{
		DamageGuideTone.Positive => Positive,
		DamageGuideTone.Warning => Warning,
		DamageGuideTone.Information => Information,
		_ => Border
	};

	internal static void Fill(SpriteBatch batch, Rectangle area, Color color)
	{
		if (area.Width > 0 && area.Height > 0)
			batch.Draw(TextureAssets.MagicPixel.Value, area, color);
	}

	internal static void DrawPanel(SpriteBatch batch, Rectangle area, Color fill, Color border)
	{
		if (area.Width < 6 || area.Height < 6)
			return;
		Fill(batch, new Rectangle(area.X + 2, area.Y, area.Width - 4, area.Height), border);
		Fill(batch, new Rectangle(area.X, area.Y + 2, area.Width, area.Height - 4), border);
		Fill(batch, new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), fill);
		Fill(batch, new Rectangle(area.X + 3, area.Y + 2, area.Width - 6, 1), Color.Lerp(fill, border, 0.35f));
	}
}
