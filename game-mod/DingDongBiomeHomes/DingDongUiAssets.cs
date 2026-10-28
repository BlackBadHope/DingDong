using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent;
using ReLogic.Graphics;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes;

internal static class DingDongUiAssets
{
	internal const int PanelCorner = 84;
	internal const int CardCorner = 56;

	// RamOpt 28.10: the book/journal sheets (~27 MB RGBA, four 1.2–2.2k sheets) load on
	// first draw instead of at mod load; every consumer already null-checks or reads .Value
	// on the main thread, where ImmediateLoad is synchronous.
	private static Mod owner;
	private static Asset<Texture2D> panel, book, residentPortraits, parchmentCard, bookRibbons,
		bookControls, card, cardReady, tab, tabSelected, close;

	internal static Asset<Texture2D> Panel => panel ??= Request("Assets/Ui/JournalPanel");
	internal static Asset<Texture2D> Book => book ??= Request("Assets/Ui/ResidentBook");
	internal static Asset<Texture2D> ResidentPortraits => residentPortraits ??= Request("Assets/Ui/Residents/CorePortraits");
	internal static Asset<Texture2D> ParchmentCard => parchmentCard ??= Request("Assets/Ui/ParchmentCard");
	internal static Asset<Texture2D> BookRibbons => bookRibbons ??= Request("Assets/Ui/BookRibbons");
	internal static Asset<Texture2D> BookControls => bookControls ??= Request("Assets/Ui/BookControls");
	internal static Asset<Texture2D> Card => card ??= Request("Assets/Ui/JournalCard");
	internal static Asset<Texture2D> CardReady => cardReady ??= Request("Assets/Ui/JournalCardReady");
	internal static Asset<Texture2D> Tab => tab ??= Request("Assets/Ui/JournalTab");
	internal static Asset<Texture2D> TabSelected => tabSelected ??= Request("Assets/Ui/JournalTabSelected");
	internal static Asset<Texture2D> Close => close ??= Request("Assets/Ui/JournalClose");

	internal static void Load(Mod mod) => owner = mod;

	internal static void Unload()
	{
		owner = null;
		panel = book = residentPortraits = parchmentCard = bookRibbons = bookControls = null;
		card = cardReady = tab = tabSelected = close = null;
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

    internal static void DrawBookCard(SpriteBatch batch,Rectangle destination,Color tint)
    {
        if(ParchmentCard==null)return;
        var texture=ParchmentCard.Value;
        // Source and destination corners have independent sizes: a short row
        // keeps its engraved border instead of stretching the whole square.
        var source=new Rectangle(35,36,1188,1181);
        const int sc=128;
        int dc=Math.Min(14,Math.Min(destination.Width,destination.Height)/3);
        for(int y=0;y<3;y++)for(int x=0;x<3;x++) {
            int sx=x==0?source.X:x==1?source.X+sc:source.Right-sc;
            int sy=y==0?source.Y:y==1?source.Y+sc:source.Bottom-sc;
            int sw=x==1?source.Width-2*sc:sc,sh=y==1?source.Height-2*sc:sc;
            int dx=x==0?destination.X:x==1?destination.X+dc:destination.Right-dc;
            int dy=y==0?destination.Y:y==1?destination.Y+dc:destination.Bottom-dc;
            int dw=x==1?destination.Width-2*dc:dc,dh=y==1?destination.Height-2*dc:dc;
            if(dw>0&&dh>0)batch.Draw(texture,new Rectangle(dx,dy,dw,dh),new Rectangle(sx,sy,sw,sh),tint);
        }
    }

	private static Asset<Texture2D> Request(string path) =>
		owner?.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
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
        if (shell && DingDongUiAssets.Book != null) { spriteBatch.Draw(DingDongUiAssets.Book.Value,area,Color.White);return; }
		if (shell)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 5, area.Y + 7, area.Width, area.Height), Color.Black * 0.35f);
        if(!shell&&DingDongUiAssets.ParchmentCard!=null)DingDongUiAssets.DrawBookCard(spriteBatch,area,Color.White);
        else DingDongUiTheme.DrawPanel(spriteBatch, area, DingDongUiTheme.Shell, DingDongUiTheme.Border);
		if (shell)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 18, area.Y, 52, 2), DingDongUiTheme.Gold);
		else if (tone != DamageGuideTone.Normal)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 1, area.Y + 8, 3, Math.Max(0, area.Height - 16)), DingDongUiTheme.Accent(tone));
	}
}

internal sealed class JournalCloseButton : UIElement
{
	internal JournalCloseButton() => SetPadding(0);

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
        BookSkin.Draw(spriteBatch,GetDimensions().ToRectangle(),BookGlyph.Close,IsMouseHovering?new Color(255,235,172):Color.White);
		if (IsMouseHovering)
			Main.hoverItemName = Terraria.Localization.Language.GetTextValue("Mods.DingDongBiomeHomes.Hub.Close");
	}
}
