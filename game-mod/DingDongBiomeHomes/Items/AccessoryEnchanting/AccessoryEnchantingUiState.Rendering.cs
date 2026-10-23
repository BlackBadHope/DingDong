using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    private static UIText Label(UIElement parent, float x, float y, float w, float h, Color color, float scale = 0.76f)
    {
        var text = new UIText(string.Empty, scale) { TextColor = color, IsWrapped = true, TextOriginX = 0, TextOriginY = 0, IgnoresMouseInteraction = true };
        text.Left.Set(x, 0); text.Top.Set(y, 0); text.Width.Set(w, 0); text.Height.Set(h, 0); parent.Append(text); return text;
    }
    private static UIPanel Card(UIElement parent, float x, float y, float w, float h)
    {
        var card = new UIPanel { BackgroundColor = new Color(29, 43, 68), BorderColor = new Color(67, 88, 119) };
        card.SetPadding(10); card.Left.Set(x, 0); card.Top.Set(y, 0); card.Width.Set(w, 0); card.Height.Set(h, 0); parent.Append(card); return card;
    }
    private static UITextPanel<string> Button(UIElement parent, float x, float y, float w, float h, MouseEvent callback)
    {
        var button = new UITextPanel<string>(string.Empty, 0.76f) { BackgroundColor = new Color(42, 80, 75), BorderColor = new Color(137, 182, 157) };
        button.SetPadding(4); button.Left.Set(x, 0); button.Top.Set(y, 0); button.Width.Set(w, 0); button.Height.Set(h, 0); button.OnLeftClick += callback; parent.Append(button); return button;
    }
    private static void SetEnabled(UITextPanel<string> button, bool enabled)
    {
        button.BackgroundColor = enabled ? new Color(37, 91, 78) : new Color(41, 48, 65);
        button.BorderColor = enabled ? Good : new Color(78, 87, 107);
        button.TextColor = enabled ? Ink : Muted;
    }
}
