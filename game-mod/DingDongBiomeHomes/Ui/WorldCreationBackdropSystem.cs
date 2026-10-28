using System;
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

// 28.10: "First Word" backdrop of the world-generation screen. The bell (the First Word) tolls,
// and the continent is lit ("spoken") outward from the bell as generation progresses.
// Art: Assets/Ui/WorldCreation (source script Tools/Art/render_world_creation_backdrop.py).
public sealed class WorldCreationBackdropSystem : ModSystem
{
	private const string AssetPath = "DingDongBiomeHomes/Assets/Ui/WorldCreation/";
	private const string TextKey = "Mods.DingDongBiomeHomes.WorldCreation.Backdrop";
	private const int LineCount = 10;
	private const float ArtWidth = 1920f, ArtHeight = 1080f;
	private static readonly Vector2 BellCore = new(960f, 392f);
	private static readonly FieldInfo ProgressBarField =
		typeof(UIWorldLoad).GetField("_progressBar", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo ProgressMessageField =
		typeof(UIWorldLoad).GetField("_progressMessage", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Stopwatch clock = new();

	private static Asset<Texture2D> backdrop, spoken, ring, glow;
	private static UIWorldLoad currentScreen;
	private static float shownProgress;
	private static bool failed;

	public override void Load()
	{
		if (Main.dedServ)
			return;
		backdrop = Request("Backdrop");
		spoken = Request("Spoken");
		ring = Request("Ring");
		glow = Request("Glow");
		clock.Restart();
		On_UIWorldLoad.DrawSelf += DrawBackdrop;
	}

	public override void Unload()
	{
		if (Main.dedServ)
			return;
		On_UIWorldLoad.DrawSelf -= DrawBackdrop;
		backdrop = spoken = ring = glow = null;
		currentScreen = null;
	}

	private static Asset<Texture2D> Request(string name) =>
		ModContent.Request<Texture2D>(AssetPath + name, AssetRequestMode.AsyncLoad);

	private static void DrawBackdrop(On_UIWorldLoad.orig_DrawSelf orig, UIWorldLoad self, SpriteBatch spriteBatch)
	{
		if (!failed && backdrop is { IsLoaded: true } && spoken.IsLoaded && ring.IsLoaded && glow.IsLoaded)
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
				DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_WORLDGEN_BACKDROP|verdict=DISABLED|reason=" + ex.Message);
			}
		}
		orig(self, spriteBatch);
	}

	// The vanilla progress bar sits in the middle of the screen, over the bell: move it down.
	private static void BeginScreen(UIWorldLoad self)
	{
		currentScreen = self;
		shownProgress = 0f;
		if (ProgressBarField?.GetValue(self) is UIElement bar && ProgressMessageField?.GetValue(self) is UIElement message)
		{
			bar.VAlign = 1f;
			bar.Top.Set(-56f, 0f);
			message.VAlign = 1f;
			message.Top.Set(-126f, 0f);
			self.Recalculate();
		}
	}

	private static void DrawLayers(UIWorldLoad self, SpriteBatch spriteBatch)
	{
		CalculatedStyle screen = self.GetDimensions();
		float scale = Math.Max(screen.Width / ArtWidth, screen.Height / ArtHeight);
		Vector2 origin = new(screen.X + (screen.Width - ArtWidth * scale) / 2f,
			screen.Y + (screen.Height - ArtHeight * scale) / 2f);
		Vector2 At(Vector2 art) => origin + art * scale;
		float time = (float)clock.Elapsed.TotalSeconds;

		Texture2D baseTexture = backdrop.Value;
		spriteBatch.Draw(baseTexture, origin, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

		// The spoken world: a band centred on the bell that widens with generation progress.
		float target = MathHelper.Clamp((float)(WorldGenerator.CurrentGenerationProgress?.TotalProgress ?? 0d), 0f, 1f);
		shownProgress += (target - shownProgress) * 0.04f;
		float eased = 1f - (1f - shownProgress) * (1f - shownProgress);
		int half = (int)(BellCore.X * MathHelper.Lerp(0.035f, 1f, eased));
		var source = new Rectangle((int)BellCore.X - half, 0, half * 2, (int)ArtHeight);
		spriteBatch.Draw(spoken.Value, At(new Vector2(source.X, 0f)), source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

		Texture2D glowTexture = glow.Value;
		Vector2 glowOrigin = glowTexture.Size() / 2f;
		// Seams where the world is being written right now.
		if (half < BellCore.X - 2)
		{
			float flicker = 0.75f + 0.25f * MathF.Sin(time * 7.3f);
			Color seam = (new Color(255, 205, 120) * (0.85f * flicker)) with { A = 0 };
			foreach (float x in new[] { BellCore.X - half, BellCore.X + half })
				spriteBatch.Draw(glowTexture, At(new Vector2(x, 820f)), null, seam, 0f, glowOrigin,
					new Vector2(0.45f, 3.2f) * scale, SpriteEffects.None, 0f);
		}

		// The bell tolls: three rings expanding from the core, alternating gold and sky blue.
		Texture2D ringTexture = ring.Value;
		Vector2 ringOrigin = ringTexture.Size() / 2f;
		const float Period = 3.4f;
		for (int k = 0; k < 3; k++)
		{
			float phase = (time / Period + k / 3f) % 1f;
			float diameter = MathHelper.Lerp(240f, 2300f, phase);
			float alpha = (1f - phase) * (1f - phase) * 0.55f;
			Color tint = (((int)(time / Period + k / 3f) + k) % 2 == 0 ? new Color(255, 200, 110) : new Color(130, 215, 255)) * alpha;
			spriteBatch.Draw(ringTexture, At(BellCore), null, tint with { A = 0 }, 0f, ringOrigin,
				new Vector2(diameter / ringTexture.Width, diameter / ringTexture.Width / 1.65f) * scale, SpriteEffects.None, 0f);
		}
		float pulse = MathF.Pow(1f - (time / Period) % 1f, 3f);
		spriteBatch.Draw(glowTexture, At(BellCore), null, (new Color(255, 225, 160) * (0.35f + 0.45f * pulse)) with { A = 0 },
			0f, glowOrigin, (2.6f + 0.6f * pulse) * scale, SpriteEffects.None, 0f);

		// Title and a rotating line of lore.
		DynamicSpriteFont titleFont = FontAssets.DeathText.Value;
		DynamicSpriteFont lineFont = FontAssets.MouseText.Value;
		float uiScale = Math.Min(1f, screen.Width / 1600f) + 0.15f;
		DrawCentered(spriteBatch, titleFont, Language.GetTextValue(TextKey + ".Title"),
			new Vector2(screen.Center().X, screen.Y + screen.Height * 0.075f), new Color(255, 214, 120), 0.95f * uiScale);
		DrawCentered(spriteBatch, lineFont, Language.GetTextValue(TextKey + ".Subtitle"),
			new Vector2(screen.Center().X, screen.Y + screen.Height * 0.075f + 50f * uiScale), new Color(200, 210, 240), 0.95f * uiScale);

		const float LineSeconds = 7f;
		int line = (int)(time / LineSeconds) % LineCount;
		float local = time % LineSeconds;
		float fade = MathHelper.Clamp(Math.Min(local / 0.9f, (LineSeconds - local) / 0.9f), 0f, 1f);
		DrawCentered(spriteBatch, lineFont, Language.GetTextValue($"{TextKey}.Line{line}"),
			new Vector2(screen.Center().X, screen.Y + screen.Height - 190f), new Color(255, 236, 200) * fade, 1.15f * uiScale);
	}

	private static void DrawCentered(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 center, Color color, float scale)
	{
		Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, center, color, 0f, size / 2f, new Vector2(scale));
	}
}
