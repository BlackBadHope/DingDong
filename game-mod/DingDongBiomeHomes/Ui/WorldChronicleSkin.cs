using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Ui;

// Shared native book surface. Art is separate from live UI text and progress.
// Only the coast, foliage and chimney move; the camera and book never move.
internal static class WorldChronicleSkin
{
    private static Asset<Texture2D> atlas, shore, smoke;
    internal static readonly Color Ink = new(57, 36, 19);
    internal static readonly Color Muted = new(105, 78, 43);
    internal static readonly Color Green = new(69, 121, 46);
    internal static bool Ready => atlas is { IsLoaded: true } && shore is { IsLoaded: true };

    internal static void Load(Mod mod)
    {
        atlas = mod.Assets.Request<Texture2D>("Assets/Ui/WorldCreation/ChronicleAtlas", AssetRequestMode.AsyncLoad);
        shore = mod.Assets.Request<Texture2D>("Assets/Ui/WorldCreation/ChronicleShore", AssetRequestMode.AsyncLoad);
        smoke = Main.Assets.Request<Texture2D>("Images/Cloud_0", AssetRequestMode.AsyncLoad);
    }

    internal static void Unload() { atlas = shore = smoke = null; }
    internal static string Local(string key) => Language.GetTextValue("Mods.DingDongBiomeHomes.WorldChronicle." + key);

    internal static Rectangle BookBounds(Rectangle screen)
    {
        Texture2D book = DingDongUiAssets.Book.Value;
        float fit = Math.Min((screen.Width - 40f) / book.Width, (screen.Height - 76f) / book.Height);
        fit = Math.Min(fit, 1.04f);
        int w = (int)(book.Width * fit), h = (int)(book.Height * fit);
        return new(screen.X + (screen.Width - w) / 2, screen.Y + 48 + (screen.Height - 76 - h) / 2, w, h);
    }

    internal static Rectangle LeftPage(Rectangle b) => new(b.X + (int)(b.Width * .105f), b.Y + (int)(b.Height * .100f), (int)(b.Width * .355f), (int)(b.Height * .755f));
    internal static Rectangle RightPage(Rectangle b) => new(b.X + (int)(b.Width * .545f), b.Y + (int)(b.Height * .100f), (int)(b.Width * .350f), (int)(b.Height * .755f));

    internal static void Draw(SpriteBatch batch, Rectangle screen, double time, bool loading = false, bool settings = false)
    {
        batch.Draw(TextureAssets.MagicPixel.Value, screen, new Color(5, 12, 24) * .88f);
        Rectangle book = BookBounds(screen), left = LeftPage(book), right = RightPage(book);
        batch.Draw(DingDongUiAssets.Book.Value, book, Color.White);
        float u = Math.Min(book.Width / 1500f, book.Height / 920f);
        Heading(batch, Language.GetTextValue("Mods.DingDongBiomeHomes.WorldCreation.Backdrop.Title"),
            new(left.X, left.Y, left.Width, (int)(46 * u)), .80f * u);
        int size = Math.Min(left.Width, left.Height - (int)((settings ? 270 : 108) * u));
        var map = new Rectangle(left.X + (left.Width - size) / 2, left.Y + (int)(54 * u), size, size);
        batch.Draw(atlas.Value, map, Color.White);
        Wrapped(batch, Local("Motto"), new Rectangle(left.X, left.Bottom - (int)((settings ? 200 : 62) * u), left.Width, (int)(62 * u)), .86f * u, Muted, true);
        DrawShore(batch, settings ? left : right, time, u);
        // A single active chapter ribbon is a label, never a fake clickable tab.
        var tab = new Rectangle(right.X + right.Width / 8, book.Y - (int)(27 * u), right.Width * 3 / 4, (int)(48 * u));
        Texture2D ribbons = DingDongUiAssets.BookRibbons.Value;
        Rectangle region = loading ? new(1100, 245, 500, 205) : new(20, 245, 520, 205);
        // Asset-relative regions are scaled from the original 2172 x 724 sheet.
        region = new((int)(region.X * ribbons.Width / 2172f), (int)(region.Y * ribbons.Height / 724f),
            (int)(region.Width * ribbons.Width / 2172f), (int)(region.Height * ribbons.Height / 724f));
        batch.Draw(ribbons, tab, region, Color.White);
        Center(batch, Local(loading ? "LoadChapter" : "CreateChapter"), new Vector2(tab.Center.X, tab.Y + 10 * u), .92f * u, new Color(255, 228, 158));
    }

    private static void DrawShore(SpriteBatch batch, Rectangle page, double time, float u)
    {
        Texture2D t = shore.Value;
        int width = page.Width + (int)(24 * u);
        int height = (int)(width * t.Height / (float)t.Width);
        var target = new Rectangle(page.X - (int)(12 * u), page.Bottom - height + (int)(35 * u), width, height);
        float sx = target.Width / (float)t.Width, sy = target.Height / (float)t.Height;
        int water = (int)(t.Height * .65f), treeRight = (int)(t.Width * .14f);
        // Draw the house and shore without distortion. Wind affects only the tree side.
        batch.Draw(t, new Rectangle(target.X + (int)(treeRight * sx), target.Y, target.Width - (int)(treeRight * sx), (int)(water * sy)),
            new Rectangle(treeRight, 0, t.Width - treeRight, water), Color.White);
        // Owner, 29.10: the motion was too faint to notice. Gusty wind: a slow swell of
        // strength under a faster sway, strongest at the crown, still at the roots.
        double gust = 3.2 + 2.2 * Math.Sin(time * .31) + 1.2 * Math.Sin(time * .87 + 1.3);
        for (int y = 0; y < water; y += 6)
        {
            int h = Math.Min(6, water - y);
            float crown = MathF.Pow(1f - y / (float)water, 1.4f);
            int drift = (int)Math.Round((Math.Sin(time * 1.55 + y * .012) * gust + Math.Sin(time * 3.7 + y * .05) * .8) * crown * u);
            batch.Draw(t, new Rectangle(target.X + drift, target.Y + (int)(y * sy), (int)(treeRight * sx), Math.Max(1, (int)((y + h) * sy) - (int)(y * sy))), new Rectangle(0, y, treeRight, h), Color.White);
        }
        for (int y = water; y < t.Height; y += 12)
        {
            int h = Math.Min(12, t.Height - y);
            batch.Draw(t, new Rectangle(target.X, target.Y + (int)(y * sy), target.Width, Math.Max(1, (int)((y + h) * sy) - (int)(y * sy))), new Rectangle(0, y, t.Width, h), Color.White);
        }
        // Ripple only open water; the rocks, house and shoreline stay planted.
        int waveX = (int)(t.Width * .20f), waveWidth = (int)(t.Width * .46f);
        for (int i = 0; i < 6; i++)
        {
            int y = (int)(t.Height * (.72f + i * .033f)), h = Math.Max(1, (int)(t.Height * .03f));
            // Alternate bands roll against each other, so the swell reads as moving water.
            int drift = (int)Math.Round((Math.Sin(time * (1.1 + i * .17) + i * 1.7) * 4.5 + Math.Sin(time * 2.6 + i) * 1.2) * (i % 2 == 0 ? 1 : -1) * u);
            batch.Draw(t, new Rectangle(target.X + (int)(waveX * sx) + drift, target.Y + (int)(y * sy), (int)(waveWidth * sx), Math.Max(1, (int)(h * sy))), new Rectangle(waveX, y, waveWidth, h), Color.White);
        }
        // Glints of light that come and go on the open water.
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        for (int i = 0; i < 16; i++)
        {
            double phase = time * (.45 + (i % 5) * .07) + i * .618;
            float life = (float)(phase % 1d), glow = MathF.Sin(life * MathF.PI);
            int cycle = (int)phase;
            float gx = waveX + ((i * 73 + cycle * 41) % 100) / 100f * waveWidth;
            float gy = t.Height * (.73f + ((i * 37 + cycle * 17) % 100) / 100f * .17f);
            int length = (int)((5 + (i % 3) * 3) * u * (.6f + .4f * glow));
            batch.Draw(pixel, new Rectangle(target.X + (int)(gx * sx) - length / 2, target.Y + (int)(gy * sy), Math.Max(2, length), Math.Max(1, (int)(1.6f * u))),
                new Color(236, 250, 255) * (.75f * glow));
        }
        DrawSmoke(batch, target, time, u);
    }

    private static void DrawSmoke(SpriteBatch batch, Rectangle scene, double time, float u)
    {
        if (smoke is not { IsLoaded: true }) return;
        Texture2D t = smoke.Value;
        // Positioned at the chimney of ChronicleShore, not at an arbitrary screen point.
        Vector2 chimney = new(scene.X + scene.Width * .900f, scene.Y + scene.Height * .328f);
        // A visible plume: more puffs that swell, lean with the wind and fade in and out.
        const int puffs = 9;
        for (int i = 0; i < puffs; i++)
        {
            float life = (float)((time * .16 + i / (double)puffs) % 1d);
            float lean = 30f + 10f * MathF.Sin((float)time * .31f);
            Vector2 position = chimney + new Vector2((life * life * lean + MathF.Sin(life * 5 + i) * 5f) * u, -life * 92f * u);
            float scale = (9 + life * 26) * u / Math.Max(t.Width, t.Height);
            float alpha = MathF.Min(1f, life * 6f) * (1 - life) * .62f;
            batch.Draw(t, position, null, new Color(150, 146, 136) * alpha, -.35f * life + i,
                new Vector2(t.Width / 2f, t.Height / 2f), scale, SpriteEffects.None, 0);
        }
    }

    internal static void SetSmoke(Asset<Texture2D> texture) => smoke = texture;
    internal static void Text(SpriteBatch batch, string text, Vector2 pos, float scale = 1f, Color? color = null) =>
        batch.DrawString(FontAssets.MouseText.Value, text ?? string.Empty, pos, color ?? Ink, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

    internal static void Center(SpriteBatch batch, string text, Vector2 pos, float scale = 1f, Color? color = null)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text ?? string.Empty);
        Text(batch, text, new Vector2(pos.X - size.X * scale / 2f, pos.Y), scale, color);
    }

    internal static void Heading(SpriteBatch batch, string text, Rectangle area, float scale = 1f)
    {
        DynamicSpriteFont font = FontAssets.DeathText.Value;
        float fit = Math.Min(scale, area.Width / Math.Max(1, font.MeasureString(text).X));
        batch.DrawString(font, text, new Vector2(area.Center.X, area.Y), Ink, 0,
            new Vector2(font.MeasureString(text).X / 2f, 0), fit, SpriteEffects.None, 0);
    }

    internal static void Wrapped(SpriteBatch batch, string text, Rectangle area, float scale, Color? color = null, bool center = false)
    {
        var font = FontAssets.MouseText.Value;
        string line = "";
        float y = area.Y, step = font.LineSpacing * scale;
        foreach (string word in (text ?? "").Split(' '))
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.MeasureString(next).X * scale > area.Width)
            {
                if (y + step > area.Bottom) return;
                if (center) Center(batch, line, new(area.Center.X, y), scale, color); else Text(batch, line, new(area.X, y), scale, color);
                line = word; y += step;
            }
            else line = next;
        }
        if (y + step <= area.Bottom)
        {
            if (center) Center(batch, line, new(area.Center.X, y), scale, color); else Text(batch, line, new(area.X, y), scale, color);
        }
    }

    internal static void Progress(SpriteBatch batch, Rectangle track, float value)
    {
        DingDongUiTheme.DrawPanel(batch, track, new Color(116, 109, 91), new Color(127, 96, 48));
        var fill = new Rectangle(track.X + 3, track.Y + 3, (int)((track.Width - 6) * MathHelper.Clamp(value, 0, 1)), Math.Max(1, track.Height - 6));
        DingDongUiTheme.Fill(batch, fill, Green);
        DingDongUiTheme.Fill(batch, new Rectangle(fill.X, fill.Y, fill.Width, Math.Min(2, fill.Height)), new Color(126, 163, 70));
    }
}
