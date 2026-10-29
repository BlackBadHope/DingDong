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
    private readonly Color ribbon;
    private readonly int ribbonIndex;
    private readonly Color ribbonTint;
	internal DingDongHubTabButton(string text, DingDongHubTab tab)
	{
		ribbon=tab switch { DingDongHubTab.Quests=>new Color(40,77,59),DingDongHubTab.Combat=>new Color(106,43,37),DingDongHubTab.Residences=>new Color(47,59,104),_=>new Color(83,46,102) };
        // The ribbon sheet has four colours; the chronicle reuses the green ribbon tinted to amber.
        ribbonIndex=tab==DingDongHubTab.Chronicle?0:(int)tab;ribbonTint=tab==DingDongHubTab.Chronicle?new Color(255,206,120):Color.White;SetPadding(8f);this.text=text;
        icon=new BookAssetIcon(tab switch { DingDongHubTab.Quests=>BookGlyph.Quest,DingDongHubTab.Combat=>BookGlyph.Combat,DingDongHubTab.Residences=>BookGlyph.Residents,DingDongHubTab.Help=>BookGlyph.Help,_=>BookGlyph.Chapter });icon.Width.Set(28,0);icon.Height.Set(28,0);icon.VAlign=.5f;Append(icon);
		label = new UIText(text, 0.98f)
		{
			IsWrapped = true,
			HAlign = 0f,
			VAlign = 0.5f,
			TextOriginX = 0.5f,
			TextOriginY = 0.5f,
			WrappedTextBottomPadding = 0f,
			TextColor = DingDongUiTheme.Muted,
			IgnoresMouseInteraction = true
		};
		label.Left.Set(28,0);label.Width.Set(-28f, 1f);
		label.Height.Set(0f, 1f);
		Append(label);
	}

	// Reference: the open tab stands taller than the others, which sit a few pixels lower.
	private const int Lowered = 5;
	private readonly BookAssetIcon icon;
	internal void SetSelected(bool value)
	{
		selected = value;
		float offset = value ? 0f : Lowered * .5f;
		icon.Top.Set(offset, 0f);
		label.Top.Set(offset, 0f); // applied by the hub's next Recalculate (SelectTab -> Refresh)
	}

	// Owner 28.10 (book screenshot): the ribbons are narrow on the right page, so
	// "Задания"/"Справка" wrapped mid-word. Fit the label to the ribbon instead.
	private readonly string text;
	public override void Recalculate()
	{
		base.Recalculate();
		if (label == null || string.IsNullOrEmpty(text)) return;
		float inner = GetInnerDimensions().Width;
		float measured = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(text).X;
		// Five ribbons share the right page: on narrow books the glyph steps aside before the
		// caption would shrink below a readable size.
		bool compact = (inner - 32f) / Math.Max(1f, measured) < 0.66f;
		icon.Width.Set(compact ? 0f : 28f, 0f);
		label.Left.Set(compact ? 0f : 28f, 0f); label.Width.Set(compact ? 0f : -28f, 1f);
		float available = Math.Max(20f, inner - (compact ? 4f : 32f));
		float scale = Math.Clamp(available / Math.Max(1f, measured), 0.55f, 0.98f);
		label.IsWrapped = false;
		label.SetText(text, scale, false);
		base.Recalculate();
	}
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		bool lit = selected || IsMouseHovering;
		Rectangle area = GetDimensions().ToRectangle();
		if (!selected && area.Height > Lowered * 3) { area.Y += Lowered; area.Height -= Lowered; }
		var source=ribbonIndex switch {0=>new Rectangle(45,243,492,203),1=>new Rectangle(568,243,496,203),2=>new Rectangle(1100,243,496,203),_=>new Rectangle(1630,242,497,204)};
        spriteBatch.Draw(DingDongUiAssets.BookRibbons.Value,area,source,lit?ribbonTint:ribbonTint*.82f);
		if (selected)
			DingDongUiTheme.Fill(spriteBatch, new Rectangle(area.X + 10, area.Bottom - 3, area.Width - 20, 2), DingDongUiTheme.Gold);
		label.TextColor = selected ? new Color(255,217,128) : new Color(244,235,215);
	}
}
