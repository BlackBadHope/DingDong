using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace DingDongBiomeHomes;

/// <summary>
/// The book's two-leaf layout shared by "Летопись", "Бой", "Жители" and "Справка":
/// a scrolling index on the left page and a scrolling page on the right one
/// (same geometry as the quest journal, so switching tabs does not jump).
/// </summary>
internal sealed class BookSpread : UIElement
{
	internal readonly BookList Index = new(6f);
	internal readonly BookList Page = new(10f);
	private readonly BookScrollbar indexScroll = new();
	private readonly BookScrollbar pageScroll = new();

	internal BookSpread()
	{
		Width.Set(0, 1); Height.Set(0, 1);
		Index.Width.Set(-26, .455f); Index.Height.Set(0, 1); Append(Index);
		indexScroll.Left.Set(-24, .455f); indexScroll.Width.Set(20, 0); indexScroll.Height.Set(0, 1);
		indexScroll.SetView(100, 1000); Append(indexScroll); Index.SetScrollbar(indexScroll);
		Page.Left.Set(0, .525f); Page.Top.Set(-60, 0); Page.Width.Set(-27, .455f); Page.Height.Set(56, 1); Append(Page);
		pageScroll.Left.Set(-22, .98f); pageScroll.Top.Set(-60, 0); pageScroll.Width.Set(20, 0); pageScroll.Height.Set(56, 1);
		pageScroll.SetView(100, 1000); Append(pageScroll); Page.SetScrollbar(pageScroll);
	}

	internal void Clear(bool keepIndexScroll)
	{
		float index = Index.ViewPosition;
		Index.Clear();
		Page.Clear();
		Page.ViewPosition = 0;
		if (!keepIndexScroll)
			Index.ViewPosition = 0;
		else
			pendingIndex = index;
	}

	private float pendingIndex = -1;

	/// <summary>Lays out both leaves and restores the index scroll after a rebuild.</summary>
	internal void Finish()
	{
		Recalculate();
		if (pendingIndex >= 0)
		{
			Index.ViewPosition = pendingIndex;
			pendingIndex = -1;
		}
	}

	internal void AddHeading(string title, string subtitle)
	{
		var heading = new BookText(title, 1.3f, ResidentBookJournal.Ink);
		Page.Add(heading);
		if (!string.IsNullOrEmpty(subtitle))
			Page.Add(new BookText(subtitle, .8f, ResidentBookJournal.Muted));
	}

	internal void AddIndexCaption(string text)
	{
		var caption = new BookCaption(text);
		Index.Add(caption);
	}
}

/// <summary>Small chapter caption with a gold rule (index of the left page).</summary>
internal sealed class BookCaption : UIElement
{
	private readonly BookText text;
	internal BookCaption(string value)
	{
		Width.Set(0, 1); Height.Set(30, 0);
		text = new BookText(value, .92f, DingDongUiTheme.Gold) { SingleLine = true };
		text.Left.Set(6, 0); text.Top.Set(4, 0); text.Width.Set(-12, 1);
		Append(text);
		IgnoresMouseInteraction = true;
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		DingDongUiTheme.Fill(batch, new Rectangle(area.X + 6, area.Bottom - 3, area.Width - 12, 2), new Color(176, 132, 62) * .8f);
	}
}

/// <summary>
/// Index row: icon slot, a single-line title (cut with "..." instead of overlapping), an
/// optional subtitle and a right-aligned badge (e.g. "новое"). Main-story rows get a gold frame.
/// </summary>
internal sealed class BookIndexRow : UIElement
{
	internal const float TallHeight = 58f, ShortHeight = 44f;
	private readonly QuestTargetIcon icon;
	private readonly BookText title, subtitle, badge;
	private readonly bool selected, muted, golden;
	private readonly float badgeWidth;

	internal BookIndexRow(string titleText, string subtitleText, int itemType, int npcType, bool selected, bool muted = false, string badgeText = null, bool golden = false)
	{
		this.selected = selected; this.muted = muted; this.golden = golden;
		bool tall = !string.IsNullOrEmpty(subtitleText);
		Width.Set(0, 1); Height.Set(tall ? TallHeight : ShortHeight, 0); SetPadding(6);
		if (itemType > ItemID.None || npcType > NPCID.None)
		{
			icon = new QuestTargetIcon(itemType, npcType, tall ? 44 : 32, npcType > NPCID.None);
			icon.Width.Set(tall ? 44 : 32, 0); icon.Height.Set(0, 1);
			Append(icon);
		}
		Color ink = muted ? new Color(120, 108, 92) : ResidentBookJournal.Ink;
		title = new BookText(titleText ?? string.Empty, .9f, ink) { SingleLine = true };
		Append(title);
		if (tall)
		{
			subtitle = new BookText(subtitleText, .72f, ResidentBookJournal.Muted) { SingleLine = true };
			Append(subtitle);
		}
		if (!string.IsNullOrEmpty(badgeText))
		{
			badge = new BookText(badgeText, .7f, new Color(170, 92, 24)) { SingleLine = true };
			badgeWidth = Math.Min(120f, FontAssets.MouseText.Value.MeasureString(badgeText).X * .7f + 6f);
			Append(badge);
		}
	}

	public override void Recalculate()
	{
		base.Recalculate();
		float left = icon != null ? icon.Width.Pixels + 8f : 4f;
		float right = badge != null ? badgeWidth + 6f : 2f;
		title.Left.Set(left, 0); title.Width.Set(-left - right, 1); title.Top.Set(subtitle != null ? 2f : 6f, 0);
		if (subtitle != null) { subtitle.Left.Set(left, 0); subtitle.Width.Set(-left - 4f, 1); subtitle.Top.Set(25f, 0); }
		if (badge != null) { badge.Left.Set(-badgeWidth, 1); badge.Width.Set(badgeWidth, 0); badge.Top.Set(subtitle != null ? 4f : 8f, 0); }
		base.Recalculate();
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		Color tint = selected ? new Color(255, 236, 177) : IsMouseHovering ? Color.White : Color.White * (muted ? .55f : .72f);
		DingDongUiAssets.DrawBookCard(batch, area, tint);
		if (golden)
			BookFrames.Gold(batch, area);
		if (selected)
			DingDongUiTheme.Fill(batch, new Rectangle(area.X + 5, area.Y + 8, 3, Math.Max(0, area.Height - 16)), new Color(196, 146, 48));
	}

	public override void MouseOver(UIMouseEvent evt)
	{
		base.MouseOver(evt);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}

/// <summary>Filter chip above the resident list; the label shrinks to fit instead of wrapping.</summary>
internal sealed class BookFilterButton : UIElement
{
	private readonly string label;
	private float scale = .78f, labelWidth;
	internal bool Selected { get; set; }

	internal BookFilterButton(string label)
	{
		this.label = label ?? string.Empty;
		Height.Set(30, 0);
	}

	public override void Recalculate()
	{
		base.Recalculate();
		float measured = Math.Max(1f, FontAssets.MouseText.Value.MeasureString(label).X);
		scale = Math.Clamp((GetDimensions().Width - 10f) / measured, .5f, .78f);
		labelWidth = measured * scale;
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		DingDongUiAssets.DrawBookCard(batch, area, Selected ? new Color(247, 214, 140) : IsMouseHovering ? Color.White : Color.White * .7f);
		if (Selected)
			DingDongUiTheme.Fill(batch, new Rectangle(area.X + 6, area.Bottom - 4, area.Width - 12, 2), new Color(176, 120, 40));
		var font = FontAssets.MouseText.Value;
		var at = new Vector2(area.X + (area.Width - labelWidth) * .5f, area.Y + (area.Height - font.LineSpacing * scale) * .5f + 2f);
		batch.DrawString(font, label, at, Selected ? ResidentBookJournal.Ink : ResidentBookJournal.Muted, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

internal static class BookFrames
{
	internal static readonly Color GoldLine = new(206, 154, 50);

	/// <summary>Two-pixel gold frame inside a card (main storyline rows and portraits).</summary>
	internal static void Gold(SpriteBatch batch, Rectangle area)
	{
		var inner = new Rectangle(area.X + 3, area.Y + 3, area.Width - 6, area.Height - 6);
		if (inner.Width < 8 || inner.Height < 8)
			return;
		DingDongUiTheme.Fill(batch, new Rectangle(inner.X, inner.Y, inner.Width, 2), GoldLine);
		DingDongUiTheme.Fill(batch, new Rectangle(inner.X, inner.Bottom - 2, inner.Width, 2), GoldLine);
		DingDongUiTheme.Fill(batch, new Rectangle(inner.X, inner.Y, 2, inner.Height), GoldLine);
		DingDongUiTheme.Fill(batch, new Rectangle(inner.Right - 2, inner.Y, 2, inner.Height), GoldLine);
	}
}

/// <summary>Horizontal bar with an arbitrary fill colour (force lean scale, main-line progress).</summary>
internal sealed class BookColorBar : UIElement
{
	private readonly float value;
	private readonly Color fill;
	internal BookColorBar(float value, Color fill, float height = 12f)
	{
		this.value = Math.Clamp(value, 0f, 1f); this.fill = fill;
		Width.Set(0, 1); Height.Set(height, 0); IgnoresMouseInteraction = true;
	}

	protected override void DrawSelf(SpriteBatch batch)
	{
		Rectangle a = GetDimensions().ToRectangle();
		DingDongUiTheme.DrawPanel(batch, a, new Color(112, 100, 76), new Color(71, 52, 29));
		a.Inflate(-2, -2);
		a.Width = (int)(a.Width * value);
		DingDongUiTheme.Fill(batch, a, fill);
	}
}

/// <summary>Label + bar + right-aligned amount on one line (lean scale rows, main-line totals).</summary>
internal sealed class BookMeterRow : UIElement
{
	private readonly BookText label, amount;
	private readonly BookColorBar bar;
	private readonly float amountWidth;
	internal BookMeterRow(string labelText, float value, Color fill, string amountText)
	{
		Width.Set(0, 1); Height.Set(26, 0);
		label = new BookText(labelText, .78f, ResidentBookJournal.Ink) { SingleLine = true }; Append(label);
		bar = new BookColorBar(value, fill, 12f); Append(bar);
		amount = new BookText(amountText, .78f, ResidentBookJournal.Ink) { SingleLine = true }; Append(amount);
		amountWidth = FontAssets.MouseText.Value.MeasureString(amountText ?? string.Empty).X * .78f + 8f;
	}

	public override void Recalculate()
	{
		base.Recalculate();
		float width = GetInnerDimensions().Width, labelWidth = Math.Clamp(width * .34f, 70f, 170f);
		label.Width.Set(labelWidth, 0); label.Top.Set(2, 0);
		bar.Left.Set(labelWidth + 6f, 0); bar.Width.Set(-labelWidth - amountWidth - 14f, 1); bar.Top.Set(7, 0);
		amount.Left.Set(-amountWidth, 1); amount.Width.Set(amountWidth, 0); amount.Top.Set(2, 0);
		base.Recalculate();
	}
}
