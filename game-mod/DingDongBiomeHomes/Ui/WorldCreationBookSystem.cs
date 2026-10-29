using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Ui;

// The book is a skin over the native form. Its controls, callbacks, options and gamepad
// points remain owned by UIWorldCreation and the existing ContinentalWorldHooks gates.
public sealed class WorldCreationBookSystem : ModSystem
{
	private static readonly FieldInfo NamePlateField = typeof(UIWorldCreation).GetField(
		"_namePlate", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo DescriptionField = typeof(UIWorldCreation).GetField(
		"_descriptionText", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Stopwatch Clock = new();
	private static FormSkin active;
	private static bool failed;

	public override void Load()
	{
		if (Main.dedServ) return;
		failed = false;
		Clock.Restart();
		On_UIWorldCreation.Draw += DrawBook;
	}

	public override void Unload()
	{
		if (!Main.dedServ) On_UIWorldCreation.Draw -= DrawBook;
		active?.Restore();
		active = null;
		Clock.Stop();
	}

	private static void DrawBook(On_UIWorldCreation.orig_Draw orig, UIWorldCreation self, SpriteBatch batch)
	{
		Rectangle screen = self.GetDimensions().ToRectangle();
		if (failed || !WorldChronicleSkin.Ready || screen.Width < 1000 || screen.Height < 600)
		{
			active?.Restore();
			orig(self, batch);
			return;
		}

		try
		{
			Rectangle page = WorldChronicleSkin.RightPage(WorldChronicleSkin.BookBounds(screen));
			// Keep the native 40px rows and controls legible instead of squeezing them into
			// a page which is too narrow at a large Terraria UI scale.
			if (page.Width < 340 || page.Height < 440)
			{
				active?.Restore();
			}
			else
			{
				if (active == null || !ReferenceEquals(active.Screen, self))
				{
					active?.Restore();
					active = FormSkin.Capture(self);
				}
				active.Apply(screen, page);
				WorldChronicleSkin.Draw(batch, screen, Clock.Elapsed.TotalSeconds, settings: true);
				string title = WorldChronicleSkin.Local("CreateTitle");
				if (title.EndsWith(".CreateTitle", StringComparison.Ordinal))
					title = Language.ActiveCulture.Name == "ru-RU" ? "Новая летопись" : "A new chronicle";
				WorldChronicleSkin.Heading(batch, title,
					new Rectangle(page.X + 12, page.Y + 6, page.Width - 24, 46), 0.75f);
			}
		}
		catch (Exception ex)
		{
			// A missing private UI contract must never block or modify world creation.
			active?.Restore();
			failed = true;
			DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_CREATION_BOOK|verdict=FALLBACK|reason=" + ex.Message);
		}
		// Do not retry native Draw if another mod throws: its handlers can have effects.
		orig(self, batch);
		try { active?.DrawLabels(batch); }
		catch (Exception ex)
		{
			active?.Restore();
			failed = true;
			DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_CREATION_BOOK|verdict=FALLBACK|reason=" + ex.Message);
		}
	}

	private sealed class FormSkin
	{
		internal readonly UIWorldCreation Screen;
		private readonly UIElement outer;
		private readonly UIPanel shell;
		private readonly StyleDimension left, top, width, height;
		private readonly float horizontal, vertical;
		private readonly List<Action> restoreStyles = new();
		private readonly List<UIPanel> panels = new();
		private readonly List<UIText> text = new();
		private readonly List<UITextPanel<LocalizedText>> textPanels = new();
		private readonly List<IGroupOptionButton> options = new();
		private readonly UISlicedImage descriptionPlate;
		private readonly UIText hint;
		private bool applied;

		private FormSkin(UIWorldCreation screen, UIElement container, UIPanel panel, UISlicedImage description)
		{
			Screen = screen;
			outer = container;
			shell = panel;
			descriptionPlate = description;
			left = outer.Left; top = outer.Top; width = outer.Width; height = outer.Height;
			horizontal = outer.HAlign; vertical = outer.VAlign;
			CaptureStyles(outer);
			if (descriptionPlate != null)
			{
				Color previous = descriptionPlate.Color;
				restoreStyles.Add(() => descriptionPlate.Color = previous);
			}
			hint = DescriptionField?.GetValue(screen) as UIText;
			if (hint != null)
			{
				bool wrapped = hint.IsWrapped;
				restoreStyles.Add(() => hint.IsWrapped = wrapped);
				if (!text.Contains(hint)) CaptureStyles(hint);
			}
		}

		internal static FormSkin Capture(UIWorldCreation screen)
		{
			if (NamePlateField?.GetValue(screen) is not UIElement name ||
				name.Parent?.Parent?.Parent is not UIPanel panel || panel.Parent is not UIElement container ||
				!ReferenceEquals(container.Parent, screen))
				throw new InvalidOperationException("Native world creation form layout changed.");
			var description = (DescriptionField?.GetValue(screen) as UIElement)?.Parent as UISlicedImage;
			return new FormSkin(screen, container, panel, description);
		}

		private void CaptureStyles(UIElement node)
		{
			if (node is UIPanel panel)
			{
				Color background = panel.BackgroundColor, border = panel.BorderColor;
				restoreStyles.Add(() => { panel.BackgroundColor = background; panel.BorderColor = border; });
				panels.Add(panel);
			}
			if (node is UIText label)
			{
				Color previous = label.TextColor, shadow = label.ShadowColor;
				restoreStyles.Add(() => { label.TextColor = previous; label.ShadowColor = shadow; });
				text.Add(label);
			}
			if (node is UITextPanel<LocalizedText> textPanel)
			{
				Color previous = textPanel.TextColor;
				restoreStyles.Add(() => textPanel.TextColor = previous);
				textPanels.Add(textPanel);
			}
			if (node is IGroupOptionButton option)
			{
				// The public interface exposes setters but no getters. Store exactly the fields
				// those setters write so fallback restores the original style, not a guess.
				foreach (string name in new[] { "_UseOverrideColors", "_overrideUnpickedColor", "_overridePickedColor",
					"_overrideOpacityPicked", "_overrideOpacityUnpicked", "_borderColor" })
				{
					FieldInfo field = FindField(node.GetType(), name) ?? throw new MissingFieldException(node.GetType().FullName, name);
					object previous = field.GetValue(node);
					restoreStyles.Add(() => field.SetValue(node, previous));
				}
				options.Add(option);
			}
			foreach (UIElement child in node.Children) CaptureStyles(child);
		}

		internal void Apply(Rectangle screen, Rectangle page)
		{
			int desiredWidth = Math.Min(600, page.Width - 20);
			float desiredLeft = page.X + (page.Width - desiredWidth) / 2f - screen.X;
			float desiredTop = page.Y + 12 - screen.Y;
			bool changed = !applied || outer.Left.Pixels != desiredLeft || outer.Top.Pixels != desiredTop || outer.Width.Pixels != desiredWidth;
			// Mark before the first write, so a partial failure is also fully restored.
			applied = true;
			outer.Left.Set(desiredLeft, 0); outer.Top.Set(desiredTop, 0);
			outer.Width.Set(desiredWidth, 0); outer.Height.Set(452, 0);
			outer.HAlign = 0; outer.VAlign = 0;
			foreach (UIPanel panel in panels)
			{
				panel.BackgroundColor = ReferenceEquals(panel, shell) ? Color.Transparent :
					panel.IsMouseHovering ? new Color(244, 222, 174) : new Color(222, 197, 150);
				panel.BorderColor = ReferenceEquals(panel, shell) ? Color.Transparent : new Color(144, 108, 57);
			}
			foreach (IGroupOptionButton option in options)
			{
				option.SetColorsBasedOnSelectionState(new Color(155, 174, 107), new Color(220, 195, 149), 1f, 1f);
				option.SetBorderColor(new Color(144, 108, 57));
			}
			// Owner, 29.10: dark ink inside Terraria's black outline smeared into blots on the
			// parchment. Book text is ink without an outline; button captions are drawn by
			// DrawLabels in the book's font, because text panels always draw an outline.
			foreach (UIText label in text) { label.TextColor = WorldChronicleSkin.Ink; label.ShadowColor = Color.Transparent; }
			foreach (UITextPanel<LocalizedText> label in textPanels) label.TextColor = Color.Transparent;
			if (descriptionPlate != null) descriptionPlate.Color = Color.Transparent;
			// Long hints (the Legendary lock) ran across both pages; keep them inside the page.
			if (hint != null && !hint.IsWrapped) { hint.IsWrapped = true; changed = true; }
			applied = true;
			if (changed) Screen.Recalculate();
		}

		internal void DrawLabels(SpriteBatch batch)
		{
			if (!applied) return;
			var font = Terraria.GameContent.FontAssets.MouseText.Value;
			foreach (UITextPanel<LocalizedText> label in textPanels)
			{
				string caption = label.Text;
				if (label.Parent == null || string.IsNullOrEmpty(caption)) continue;
				Rectangle box = label.GetInnerDimensions().ToRectangle();
				Vector2 size = font.MeasureString(caption);
				float scale = Math.Min(1.15f, Math.Min((box.Width - 8) / Math.Max(1f, size.X), box.Height / Math.Max(1f, size.Y * .8f)));
				Color ink = label.IsMouseHovering ? WorldChronicleSkin.Green : WorldChronicleSkin.Ink;
				batch.DrawString(font, caption, new Vector2(box.Center.X, box.Center.Y + 3), ink, 0,
					new Vector2(size.X / 2f, size.Y / 2f), scale, SpriteEffects.None, 0);
			}
		}

		internal void Restore()
		{
			if (!applied) return;
			outer.Left = left; outer.Top = top; outer.Width = width; outer.Height = height;
			outer.HAlign = horizontal; outer.VAlign = vertical;
			foreach (Action restore in restoreStyles) restore();
			applied = false;
			Screen.Recalculate();
		}

		private static FieldInfo FindField(Type type, string name)
		{
			for (; type != null; type = type.BaseType)
			{
				FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null) return field;
			}
			return null;
		}
	}
}
