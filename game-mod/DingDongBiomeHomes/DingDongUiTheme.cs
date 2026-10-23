using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace DingDongBiomeHomes;

// Shared, deliberately quiet surfaces. Pixel borders stay crisp at Terraria's UI scale.
internal static class DingDongUiTheme
{
	internal static readonly Color Text = new(236, 233, 222);
	internal static readonly Color Muted = new(160, 180, 184);
	internal static readonly Color Gold = new(225, 187, 107);
	internal static readonly Color Positive = new(113, 207, 164);
	internal static readonly Color Information = new(119, 191, 219);
	internal static readonly Color Warning = new(240, 184, 108);
	internal static readonly Color Shell = new(17, 29, 35);
	internal static readonly Color Surface = new(28, 43, 49);
	internal static readonly Color Raised = new(36, 54, 61);
	internal static readonly Color Border = new(67, 89, 96);

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
