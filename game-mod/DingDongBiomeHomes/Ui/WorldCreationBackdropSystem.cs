using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Ui;

// 28.10: "First Word" backdrop of the world-generation screen.
// 29.10 (owner: "the animation looks awful, and there is no proper status — how long, which
// stage"): the glow, rings and light band are gone; a calm backdrop carries one status panel
// with the current stage (n of m and its message), a progress bar, elapsed time and an
// estimate of the time left, plus a slowly rotating line of lore.
public sealed class WorldCreationBackdropSystem : ModSystem
{
	private const string AssetPath = "DingDongBiomeHomes/Assets/Ui/WorldCreation/";
	private const string TextKey = "Mods.DingDongBiomeHomes.WorldCreation.Backdrop";
	private const int LineCount = 10;
	private const float ArtWidth = 1920f, ArtHeight = 1080f;
	private static readonly FieldInfo ProgressBarField =
		typeof(UIWorldLoad).GetField("_progressBar", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo ProgressMessageField =
		typeof(UIWorldLoad).GetField("_progressMessage", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo GeneratorField =
		typeof(WorldGen).GetField("_generator", BindingFlags.Static | BindingFlags.NonPublic);
	private static readonly FieldInfo PassesField =
		typeof(WorldGenerator).GetField("_passes", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Stopwatch clock = new(), generation = new();

	private static Asset<Texture2D> backdrop;
	private static UIWorldLoad currentScreen;
	private static double etaSeconds = -1;
	private static bool failed;

	public override void Load()
	{
		if (Main.dedServ)
			return;
		backdrop = ModContent.Request<Texture2D>(AssetPath + "Backdrop", AssetRequestMode.AsyncLoad);
		clock.Restart();
		On_UIWorldLoad.DrawSelf += DrawBackdrop;
	}

	public override void Unload()
	{
		if (Main.dedServ)
			return;
		On_UIWorldLoad.DrawSelf -= DrawBackdrop;
		backdrop = null;
		currentScreen = null;
	}

	private static void DrawBackdrop(On_UIWorldLoad.orig_DrawSelf orig, UIWorldLoad self, SpriteBatch spriteBatch)
	{
		if (!failed && backdrop is { IsLoaded: true })
		{
			try
			{
				if (!ReferenceEquals(currentScreen, self))
					BeginScreen(self);
				DrawLayers(self, spriteBatch);
			}
			catch (Exception ex)
			{
				// Never break world generation over a decoration: fall back to the vanilla screen.
				failed = true;
				RestoreVanilla(self);
				DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_WORLDGEN_BACKDROP|verdict=DISABLED|reason=" + ex.Message);
			}
		}
		orig(self, spriteBatch);
	}

	// Our panel replaces the vanilla bar and message: move them out of sight.
	private static void BeginScreen(UIWorldLoad self)
	{
		currentScreen = self;
		etaSeconds = -1;
		shown = 0f;
		generation.Restart();
		if (ProgressBarField?.GetValue(self) is UIElement bar && ProgressMessageField?.GetValue(self) is UIElement message)
		{
			bar.Top.Set(-5000f, 0f);
			message.Top.Set(-5000f, 0f);
			self.Recalculate();
		}
	}

	private static void RestoreVanilla(UIWorldLoad self)
	{
		if (ProgressBarField?.GetValue(self) is UIElement bar && ProgressMessageField?.GetValue(self) is UIElement message)
		{
			bar.Top.Set(-60f, 0.5f);
			message.Top.Set(-120f, 0.5f);
			self.Recalculate();
		}
	}

	private static void DrawLayers(UIWorldLoad self, SpriteBatch spriteBatch)
	{
		CalculatedStyle screen = self.GetDimensions();
		float time = (float)clock.Elapsed.TotalSeconds;

		// Calm backdrop: the art slowly breathes, dimmed so the text stays readable.
		float breathe = 1f + 0.015f * (0.5f + 0.5f * MathF.Sin(time * 0.25f));
		float scale = Math.Max(screen.Width / ArtWidth, screen.Height / ArtHeight) * breathe;
		Vector2 origin = new(screen.X + (screen.Width - ArtWidth * scale) / 2f, screen.Y + (screen.Height - ArtHeight * scale) / 2f);
		spriteBatch.Draw(backdrop.Value, origin, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, new Rectangle((int)screen.X, (int)screen.Y, (int)screen.Width, (int)screen.Height), Color.Black * 0.35f);

		DynamicSpriteFont titleFont = FontAssets.DeathText.Value;
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		float ui = Math.Clamp(screen.Width / 1920f, 0.7f, 1.2f);
		DrawCentered(spriteBatch, titleFont, Language.GetTextValue(TextKey + ".Title"),
			new Vector2(screen.Center().X, screen.Y + screen.Height * 0.12f), new Color(255, 214, 120), 0.8f * ui);
		DrawCentered(spriteBatch, font, Language.GetTextValue(TextKey + ".Subtitle"),
			new Vector2(screen.Center().X, screen.Y + screen.Height * 0.12f + 46f * ui), new Color(200, 210, 240), 1f * ui);

		// Status panel.
		GenerationProgress progress = WorldGenerator.CurrentGenerationProgress;
		float p = MathHelper.Clamp((float)(progress?.TotalProgress ?? 0d), 0f, 1f);
		(int stage, int stages) = StageOf(progress);
		int width = (int)Math.Min(screen.Width * 0.7f, 980f * ui), height = (int)(150 * ui);
		var panel = new Rectangle((int)(screen.Center().X - width / 2f), (int)(screen.Y + screen.Height * 0.66f), width, height);
		spriteBatch.Draw(pixel, panel, new Color(10, 12, 26) * 0.85f);
		DrawFrame(spriteBatch, pixel, panel, new Color(255, 200, 110) * 0.65f, 2);
		int pad = (int)(22 * ui);

		string message = progress?.Message;
		if (string.IsNullOrWhiteSpace(message)) message = Language.GetTextValue(TextKey + ".Preparing");
		string stageText = stages > 0
			? Language.GetTextValue(TextKey + ".Stage", stage, stages, message)
			: message;
		DrawLeft(spriteBatch, font, Fit(font, stageText, panel.Width - 2 * pad, 1.0f * ui), new Vector2(panel.X + pad, panel.Y + pad), Color.White, 1.0f * ui);

		var track = new Rectangle(panel.X + pad, panel.Y + (int)(62 * ui), panel.Width - 2 * pad - (int)(90 * ui), (int)(18 * ui));
		spriteBatch.Draw(pixel, track, new Color(40, 44, 70));
		shown = shown <= 0f ? p : shown + (p - shown) * 0.08f;
		spriteBatch.Draw(pixel, new Rectangle(track.X, track.Y, (int)(track.Width * shown), track.Height), new Color(255, 196, 92));
		DrawFrame(spriteBatch, pixel, track, new Color(255, 220, 160) * 0.5f, 1);
		DrawLeft(spriteBatch, font, $"{p * 100f:0}%", new Vector2(track.Right + 14 * ui, track.Y - 4 * ui), new Color(255, 214, 120), 1.05f * ui);

		double elapsed = generation.Elapsed.TotalSeconds;
		if (p > 0.02f)
		{
			double raw = elapsed * (1 - p) / p;
			etaSeconds = etaSeconds < 0 ? raw : etaSeconds * 0.98 + raw * 0.02;
		}
		string timeText = Language.GetTextValue(TextKey + ".Elapsed", Clock(elapsed)) + "  ·  " +
			(p > 0.02f ? Language.GetTextValue(TextKey + ".Remaining", Clock(Math.Round(etaSeconds / 10) * 10)) : Language.GetTextValue(TextKey + ".Estimating"));
		DrawLeft(spriteBatch, font, timeText, new Vector2(panel.X + pad, panel.Y + (int)(98 * ui)), new Color(200, 210, 240), 0.9f * ui);
		string hint = Language.GetTextValue(TextKey + ".Hint");
		Vector2 hintSize = ChatManager.GetStringSize(font, hint, Vector2.One) * 0.8f * ui;
		DrawLeft(spriteBatch, font, hint, new Vector2(panel.Right - pad - hintSize.X, panel.Y + (int)(100 * ui)), new Color(150, 160, 190), 0.8f * ui);

		// A line of lore under the panel, changing every few seconds.
		const float LineSeconds = 8f;
		int line = (int)(time / LineSeconds) % LineCount;
		float local = time % LineSeconds;
		float fade = MathHelper.Clamp(Math.Min(local / 1.2f, (LineSeconds - local) / 1.2f), 0f, 1f);
		DrawCentered(spriteBatch, font, Language.GetTextValue($"{TextKey}.Line{line}"),
			new Vector2(screen.Center().X, panel.Bottom + 40f * ui), new Color(255, 236, 200) * (0.85f * fade), 1.0f * ui);
	}

	private static float shown;

	// Current pass (1-based) and the number of enabled passes, from the running generator.
	private static (int Stage, int Stages) StageOf(GenerationProgress progress)
	{
		if (progress == null || GeneratorField?.GetValue(null) is not WorldGenerator generator || PassesField?.GetValue(generator) is not IList passes)
			return (0, 0);
		double total = 0;
		foreach (GenPass pass in passes) if (pass.Enabled) total += pass.Weight;
		if (total <= 0) return (0, 0);
		double done = progress.TotalProgress * total, sum = 0;
		int index = 0, count = 0;
		foreach (GenPass pass in passes)
		{
			if (!pass.Enabled) continue;
			count++;
			if (index == 0 && sum + pass.Weight >= done) index = count;
			sum += pass.Weight;
		}
		return (Math.Max(1, index == 0 ? count : index), count);
	}

	private static string Clock(double seconds)
	{
		var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
		return span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"mm\:ss");
	}

	private static string Fit(DynamicSpriteFont font, string text, float width, float scale)
	{
		if (ChatManager.GetStringSize(font, text, Vector2.One).X * scale <= width) return text;
		while (text.Length > 4 && ChatManager.GetStringSize(font, text + "…", Vector2.One).X * scale > width)
			text = text[..^1];
		return text + "…";
	}

	private static void DrawFrame(SpriteBatch spriteBatch, Texture2D pixel, Rectangle r, Color color, int thickness)
	{
		spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
		spriteBatch.Draw(pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
	}

	private static void DrawLeft(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float scale) =>
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, position, color, 0f, Vector2.Zero, new Vector2(scale));

	private static void DrawCentered(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 center, Color color, float scale)
	{
		Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, center, color, 0f, size / 2f, new Vector2(scale));
	}
}
