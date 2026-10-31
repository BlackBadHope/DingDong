using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.Keyring;

/// <summary>A parchment inventory of stored keys. Storage and Take semantics remain in KeyringItem.</summary>
public sealed class KeyringUi : ModSystem
{
	private const int Cell = 44, Padding = 16, Header = 64, Footer = 64, Gutter = 16;
	private static readonly Color Ink = DingDongUiTheme.Text;
	private static KeyringItem open;
	private static int firstRow;
	private static ulong lastScrollTick = ulong.MaxValue;

	internal static void Toggle(KeyringItem ring)
	{
		open = ReferenceEquals(open, ring) ? null : ring;
		firstRow = 0;
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (index >= 0) layers.Insert(index, new LegacyGameInterfaceLayer("DingDong: Keyring", Draw, InterfaceScaleType.UI));
	}

	private static bool Draw()
	{
		if (open == null) return true;
		if (!Main.playerInventory || Main.gameMenu || KeyringItem.Find(Main.LocalPlayer) == null)
		{
			open = null; firstRow = 0; return true;
		}
		// Escape remains available to vanilla inventory handling; do not consume or rewrite keyboard state.
		if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
		{
			Close(); return true;
		}
		KeyringItem ring = KeyringItem.Find(Main.LocalPlayer);
		if (!ReferenceEquals(ring, open)) { open = ring; firstRow = 0; }
		var groups = open.Stored.GroupBy(i => i.type).Select(g => (Type: g.Key, Stack: g.Sum(i => i.stack))).ToList();

		// GameInterfaceLayer's UI scale mode already calls PlayerInput.SetZoom_UI: screenWidth,
		// screenHeight and MouseScreen are in UI coordinates here. A second division breaks hit testing.
		Layout layout = CalculateLayout(Main.screenWidth, Main.screenHeight, groups.Count, firstRow);
		firstRow = layout.FirstRow;
		Point mouse = Main.MouseScreen.ToPoint();
		bool hovered = layout.Panel.Contains(mouse);
		if (hovered)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.LocalPlayer.cursorItemIconEnabled = false;
			PlayerInput.LockVanillaMouseScroll("DingDong: Keyring");
			if (lastScrollTick != Main.GameUpdateCount && PlayerInput.ScrollWheelDeltaForUI != 0)
			{
				lastScrollTick = Main.GameUpdateCount;
				int delta = PlayerInput.ScrollWheelDeltaForUI;
				int steps = Math.Max(1, Math.Abs(delta) / 120);
				firstRow = Math.Clamp(firstRow - Math.Sign(delta) * steps, 0, layout.MaxFirstRow);
				layout = CalculateLayout(Main.screenWidth, Main.screenHeight, groups.Count, firstRow);
			}
		}

		SpriteBatch batch = Main.spriteBatch;
		DingDongUiAssets.DrawBookCard(batch, layout.Panel, Color.White);
		string title = Ui("PanelTitle", "Связка ключей", "Keyring");
		DrawText(batch, title, new Vector2(layout.Panel.X + Padding, layout.Panel.Y + 10), .96f, Ink, layout.Panel.Width - 76);
		string summary = Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.Title", groups.Count, open.Stored.Sum(i => i.stack));
		DrawText(batch, summary, new Vector2(layout.Panel.X + Padding, layout.Panel.Y + 36), .68f, DingDongUiTheme.Muted, layout.Panel.Width - Padding * 2);
		Rectangle close = new(layout.Panel.Right - 38, layout.Panel.Top + 9, 26, 26);
		BookSkin.Draw(batch, close, BookGlyph.Close, close.Contains(mouse) ? new Color(255, 228, 158) : Color.White);
		if (close.Contains(mouse))
		{
			Main.hoverItemName = Ui("CloseHint", "Закрыть — Esc", "Close — Esc");
			if (Click()) { Close(); return true; }
		}

		if (groups.Count == 0)
		{
			DrawText(batch, Ui("Empty", "Ключей пока нет", "No keys stored yet"),
				new Vector2(layout.Grid.X + 2, layout.Grid.Y + 10), .86f, Ink, layout.Grid.Width - 4);
			DrawWrapped(batch, Ui("EmptyHint", "ПКМ по связке с ключом на курсоре — положить его внутрь.",
				"Right-click the keyring with a key on the cursor to store it."),
				new Rectangle(layout.Grid.X, layout.Grid.Bottom + 4, layout.Grid.Width, Footer - 12), .67f, DingDongUiTheme.Muted);
			return true;
		}

		int first = layout.FirstRow * layout.Columns;
		int end = Math.Min(groups.Count, first + layout.VisibleRows * layout.Columns);
		for (int n = first; n < end; n++)
		{
			int local = n - first;
			Rectangle box = new(layout.Grid.X + local % layout.Columns * Cell + 2,
				layout.Grid.Y + local / layout.Columns * Cell + 2, 40, 40);
			bool over = box.Contains(mouse);
			DingDongUiAssets.DrawBookCard(batch, box, over ? new Color(255, 237, 187) : new Color(246, 230, 193));
			Item shown = new(groups[n].Type); shown.stack = groups[n].Stack;
			// Keep native item animation, colour and mod inventory draw hooks; only the blue slot is replaced.
			ItemSlot.DrawItemIcon(shown, ItemSlot.Context.BankItem, batch, box.Center.ToVector2() - new Vector2(0, 2), 1f, 27f, Color.White);
			if (shown.stack > 1)
			{
				string amount = shown.stack.ToString();
				Vector2 size = FontAssets.ItemStack.Value.MeasureString(amount) * .58f;
				batch.DrawString(FontAssets.ItemStack.Value, amount, new Vector2(box.Right - size.X - 3, box.Bottom - size.Y - 1), Ink, 0f, Vector2.Zero, .58f, SpriteEffects.None, 0f);
			}
			if (!over) continue;
			Main.LocalPlayer.mouseInterface = true;
			Main.HoverItem = shown.Clone();
			Main.hoverItemName = shown.Name + "\n" + Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.TakeHint");
			// Same Take operation and preconditions as the source UI: one, or a maxStack with left Shift.
			if (Main.mouseLeft && Main.mouseLeftRelease && Main.mouseItem.IsAir && !InventoryTransactionGuard.IsBlocked(Main.LocalPlayer))
			{
				Main.mouseLeftRelease = false;
				int count = Main.keyState.IsKeyDown(Keys.LeftShift) ? shown.maxStack : 1;
				Item taken = open.Take(groups[n].Type, count);
				if (!taken.IsAir) { Main.mouseItem = taken; SoundEngine.PlaySound(SoundID.Grab); }
			}
		}

		DrawText(batch, Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.TakeHint"),
			new Vector2(layout.Grid.X, layout.Grid.Bottom + 7), .65f, DingDongUiTheme.Muted, layout.Grid.Width);
		if (layout.MaxFirstRow > 0)
		{
			DrawScrollbar(batch, layout, mouse);
			Rectangle previous = new(layout.Panel.X + Padding, layout.Grid.Bottom + 29, 74, 26);
			Rectangle next = new(layout.Panel.Right - Padding - 74, previous.Y, 74, 26);
			if (Button(batch, previous, Ui("Previous", "Выше", "Previous"), firstRow > 0, mouse)) firstRow = Math.Max(0, firstRow - layout.VisibleRows);
			if (Button(batch, next, Ui("Next", "Ниже", "Next"), firstRow < layout.MaxFirstRow, mouse)) firstRow = Math.Min(layout.MaxFirstRow, firstRow + layout.VisibleRows);
			string range = $"{first + 1}–{end} / {groups.Count}";
			float rangeWidth = FontAssets.MouseText.Value.MeasureString(range).X * .71f;
			DrawText(batch, range, new Vector2(layout.Panel.Center.X - rangeWidth / 2, previous.Y + 5), .71f, Ink, layout.Panel.Width - 190);
		}
		else
		{
			DrawText(batch, Ui("EmptyHint", "ПКМ по связке с ключом на курсоре — положить его внутрь.",
				"Right-click the keyring with a key on the cursor to store it."),
				new Vector2(layout.Grid.X, layout.Grid.Bottom + 30), .59f, DingDongUiTheme.Muted, layout.Grid.Width);
		}
		return true;
	}

	internal readonly struct Layout
	{
		internal readonly Rectangle Panel, Grid;
		internal readonly int Columns, VisibleRows, FirstRow, MaxFirstRow, TotalRows;
		internal Layout(Rectangle panel, Rectangle grid, int columns, int rows, int first, int max, int total)
		{ Panel = panel; Grid = grid; Columns = columns; VisibleRows = rows; FirstRow = first; MaxFirstRow = max; TotalRows = total; }
	}

	internal static Layout CalculateLayout(int uiWidth, int uiHeight, int count, int requestedRow)
	{
		int columns = Math.Clamp((uiWidth - 24 - Padding * 2 - Gutter) / Cell, 1, 8);
		columns = Math.Min(columns, Math.Max(4, count));
		int total = Math.Max(1, (count + columns - 1) / columns);
		int visible = Math.Min(total, Math.Clamp((uiHeight - 24 - Header - Footer) / Cell, 1, 5));
		int max = Math.Max(0, total - visible), first = Math.Clamp(requestedRow, 0, max);
		int width = Padding * 2 + columns * Cell + Gutter, height = Header + visible * Cell + Footer;
		int x = Math.Max(4, (uiWidth - width) / 2);
		int y = Math.Clamp(140, 12, Math.Max(12, uiHeight - height - 12));
		var panel = new Rectangle(x, y, width, height);
		return new Layout(panel, new Rectangle(x + Padding, y + Header, columns * Cell, visible * Cell), columns, visible, first, max, total);
	}

	private static void DrawScrollbar(SpriteBatch batch, Layout layout, Point mouse)
	{
		var track = new Rectangle(layout.Grid.Right + 3, layout.Grid.Y + 2, 9, layout.Grid.Height - 4);
		DingDongUiTheme.DrawPanel(batch, track, new Color(191, 166, 120), DingDongUiTheme.Border);
		int height = Math.Max(18, (int)(track.Height * layout.VisibleRows / (float)layout.TotalRows));
		int top = track.Y + (int)((track.Height - height) * layout.FirstRow / (float)layout.MaxFirstRow);
		DingDongUiTheme.DrawPanel(batch, new Rectangle(track.X + 1, top, track.Width - 2, height), DingDongUiTheme.Gold, DingDongUiTheme.Border);
		if (track.Contains(mouse) && Click())
		{
			float fraction = Math.Clamp((mouse.Y - track.Y - height / 2f) / Math.Max(1, track.Height - height), 0f, 1f);
			firstRow = (int)MathF.Round(fraction * layout.MaxFirstRow);
		}
	}

	private static bool Button(SpriteBatch batch, Rectangle area, string text, bool enabled, Point mouse)
	{
		bool hover = enabled && area.Contains(mouse);
		BookSkin.ActionFrame(batch, area, enabled ? hover ? new Color(255, 232, 170) : Color.White : Color.White * .55f);
		float width = FontAssets.MouseText.Value.MeasureString(text).X * .64f;
		DrawText(batch, text, new Vector2(area.Center.X - width / 2, area.Y + 5), .64f, enabled ? Ink : DingDongUiTheme.Muted, area.Width - 10);
		return hover && Click();
	}

	private static bool Click()
	{
		if (!Main.mouseLeft || !Main.mouseLeftRelease) return false;
		Main.mouseLeftRelease = false; return true;
	}
	private static void Close() { open = null; firstRow = 0; SoundEngine.PlaySound(SoundID.MenuClose); }
	private static string Ui(string key, string ru, string en)
	{
		string full = "Mods.DingDongBiomeHomes.Keyring." + key, value = Language.GetTextValue(full);
		return value == full ? Language.ActiveCulture.Name.StartsWith("ru", StringComparison.Ordinal) ? ru : en : value;
	}
	private static void DrawText(SpriteBatch batch, string text, Vector2 at, float scale, Color color, float width)
	{
		var font = FontAssets.MouseText.Value;
		if (font.MeasureString(text).X * scale > width)
		{
			while (text.Length > 1 && font.MeasureString(text + "...").X * scale > width) text = text[..^1];
			text += "...";
		}
		batch.DrawString(font, text, at, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
	private static void DrawWrapped(SpriteBatch batch, string text, Rectangle area, float scale, Color color)
	{
		var font = FontAssets.MouseText.Value; string line = ""; float y = area.Y, step = font.LineSpacing * scale;
		foreach (string word in text.Split(' '))
		{
			string candidate = line.Length == 0 ? word : line + " " + word;
			if (line.Length > 0 && font.MeasureString(candidate).X * scale > area.Width)
			{
				if (y + step > area.Bottom) return;
				DrawText(batch, line, new Vector2(area.X, y), scale, color, area.Width); line = word; y += step;
			}
			else line = candidate;
		}
		if (line.Length > 0 && y + step <= area.Bottom) DrawText(batch, line, new Vector2(area.X, y), scale, color, area.Width);
	}

	public override void Unload() { open = null; firstRow = 0; lastScrollTick = ulong.MaxValue; }
}
