using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace DingDongBiomeHomes;

// Signed standing is not a completion percentage: zero has no fill. Negative standing
// grows left from the neutral marker, positive standing grows right. This is presentation
// only; the reputation thresholds and saved player/resident values are unchanged.
internal sealed class BookStandingBar : UIElement
{
    private readonly int standing;
    private static readonly Color Negative = new(162, 75, 53);

    internal BookStandingBar(int value)
    {
        standing = Math.Clamp(value, ResidentReputationSystem.Min, ResidentReputationSystem.Max);
        Width.Set(0, 1); Height.Set(16, 0);
    }

    internal static string Caption => Text("Standing.Caption", "Отношение к вам", "Attitude toward you");
    internal static string FormatStanding(int value) =>
        Math.Clamp(value, ResidentReputationSystem.Min, ResidentReputationSystem.Max).ToString("+0;-0;0", CultureInfo.CurrentCulture);
    internal static Color StandingColor(int value) => value < 0 ? Negative : value > 0 ? DingDongUiTheme.Positive : DingDongUiTheme.Muted;

    protected override void DrawSelf(SpriteBatch batch)
    {
        Rectangle track = GetDimensions().ToRectangle();
        DingDongUiTheme.DrawPanel(batch, track, new Color(191, 170, 132), DingDongUiTheme.Border);
        Rectangle inner = track; inner.Inflate(-2, -2);
        if (inner.Width < 2 || inner.Height < 1) return;
        Rectangle fill = FillBounds(inner, standing);
        DingDongUiTheme.Fill(batch, fill, StandingColor(standing));
        // The neutral mark remains visible on an otherwise empty bar and over either fill.
        DingDongUiTheme.Fill(batch, new Rectangle(inner.X + inner.Width / 2, inner.Y, 1, inner.Height), DingDongUiTheme.Text);
        if (IsMouseHovering)
            Main.hoverItemName = Text("Standing.RangeHint",
                "От {0} до +{1}. Ноль — нейтральное отношение. Слева — неприязнь, справа — доверие.",
                "From {0} to +{1}. Zero is neutral. Dislike grows to the left; trust grows to the right.",
                ResidentReputationSystem.Min, ResidentReputationSystem.Max);
    }

    // Kept as a pure geometry calculation so an isolated probe can check zero, signs,
    // clamping and odd-width tracks without starting a client or changing reputation.
    internal static Rectangle FillBounds(Rectangle inner, int value)
    {
        int center = inner.X + Math.Max(0, inner.Width) / 2;
        int clamped = Math.Clamp(value, ResidentReputationSystem.Min, ResidentReputationSystem.Max);
        if (inner.Width <= 0 || inner.Height <= 0 || clamped == 0)
            return new Rectangle(center, inner.Y, 0, Math.Max(0, inner.Height));
        if (clamped < 0)
        {
            int width = (int)Math.Round((center - inner.Left) * (-clamped / (double)Math.Max(1, -ResidentReputationSystem.Min)));
            return new Rectangle(center - width, inner.Y, width, inner.Height);
        }
        else
        {
            int width = (int)Math.Round((inner.Right - center) * (clamped / (double)Math.Max(1, ResidentReputationSystem.Max)));
            return new Rectangle(center, inner.Y, width, inner.Height);
        }
    }

    private static string Text(string key, string ru, string en, params object[] args)
    {
        string full = "Mods.DingDongBiomeHomes.Design." + key;
        string value = Language.GetTextValue(full);
        if (value == full) value = Language.ActiveCulture.Name.StartsWith("ru", StringComparison.Ordinal) ? ru : en;
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }
}
