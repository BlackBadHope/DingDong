using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;

internal static class DingDongUiAssets
{
	internal const int PanelCorner = 84;
	internal const int CardCorner = 56;

	internal static Asset<Texture2D> Panel { get; private set; }
	internal static Asset<Texture2D> Card { get; private set; }
	internal static Asset<Texture2D> CardReady { get; private set; }
	internal static Asset<Texture2D> Tab { get; private set; }
	internal static Asset<Texture2D> TabSelected { get; private set; }
	internal static Asset<Texture2D> Close { get; private set; }

	internal static void Load(Mod mod)
	{
		Panel = Request(mod, "Assets/Ui/JournalPanel");
		Card = Request(mod, "Assets/Ui/JournalCard");
		CardReady = Request(mod, "Assets/Ui/JournalCardReady");
		Tab = Request(mod, "Assets/Ui/JournalTab");
		TabSelected = Request(mod, "Assets/Ui/JournalTabSelected");
		Close = Request(mod, "Assets/Ui/JournalClose");
	}

	internal static void Unload()
	{
		Panel = null;
		Card = null;
		CardReady = null;
		Tab = null;
		TabSelected = null;
		Close = null;
	}

	internal static void DrawNine(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, int corner, Color color)
	{
		int width = texture.Width;
		int height = texture.Height;
		corner = Math.Clamp(corner, 1, Math.Min(width, height) / 2 - 1);
		int middleSourceWidth = width - corner * 2;
		int middleSourceHeight = height - corner * 2;
		int middleDestWidth = destination.Width - corner * 2;
		int middleDestHeight = destination.Height - corner * 2;
		if (middleDestWidth < 1 || middleDestHeight < 1)
		{
			spriteBatch.Draw(texture, destination, color);
			return;
		}

		int x = destination.X;
		int y = destination.Y;
		void Patch(int sourceX, int sourceY, int sourceWidth, int sourceHeight, int destX, int destY, int destWidth, int destHeight)
		{
			spriteBatch.Draw(
				texture,
				new Rectangle(destX, destY, destWidth, destHeight),
				new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
				color);
		}

		Patch(0, 0, corner, corner, x, y, corner, corner);
		Patch(corner, 0, middleSourceWidth, corner, x + corner, y, middleDestWidth, corner);
		Patch(width - corner, 0, corner, corner, x + corner + middleDestWidth, y, corner, corner);
		Patch(0, corner, corner, middleSourceHeight, x, y + corner, corner, middleDestHeight);
		Patch(corner, corner, middleSourceWidth, middleSourceHeight, x + corner, y + corner, middleDestWidth, middleDestHeight);
		Patch(width - corner, corner, corner, middleSourceHeight, x + corner + middleDestWidth, y + corner, corner, middleDestHeight);
		Patch(0, height - corner, corner, corner, x, y + corner + middleDestHeight, corner, corner);
		Patch(corner, height - corner, middleSourceWidth, corner, x + corner, y + corner + middleDestHeight, middleDestWidth, corner);
		Patch(width - corner, height - corner, corner, corner, x + corner + middleDestWidth, y + corner + middleDestHeight, corner, corner);
	}

	private static Asset<Texture2D> Request(Mod mod, string path) =>
		mod.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
}

internal class JournalFrame : UIElement
{
	private readonly bool shell;
	private readonly DamageGuideTone tone;

	internal JournalFrame(DamageGuideTone tone = DamageGuideTone.Normal, bool shell = false)
	{
		this.tone = tone;
		this.shell = shell;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		if (shell)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 5, area.Y + 7, area.Width, area.Height), Color.Black * 0.35f);
		DingDongUiTheme.DrawPanel(spriteBatch, area, shell ? DingDongUiTheme.Shell : DingDongUiTheme.Surface, DingDongUiTheme.Border);
		if (shell)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 18, area.Y, 52, 2), DingDongUiTheme.Gold);
		else if (tone != DamageGuideTone.Normal)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 1, area.Y + 8, 3, Math.Max(0, area.Height - 16)), DingDongUiTheme.Accent(tone));
	}
}

internal sealed class JournalCloseButton : UIElement
{
	private readonly UIText mark;

	internal JournalCloseButton()
	{
		mark = new UIText("×", 0.92f)
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextColor = DingDongUiTheme.Muted,
			IgnoresMouseInteraction = true
		};
		Append(mark);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		DingDongUiTheme.DrawPanel(spriteBatch, GetDimensions().ToRectangle(), IsMouseHovering ? DingDongUiTheme.Raised : DingDongUiTheme.Surface, IsMouseHovering ? DingDongUiTheme.Gold : DingDongUiTheme.Border);
		mark.TextColor = IsMouseHovering ? DingDongUiTheme.Text : DingDongUiTheme.Muted;
		if (IsMouseHovering)
			Main.hoverItemName = Terraria.Localization.Language.GetTextValue("Mods.DingDongBiomeHomes.Hub.Close");
	}
}
