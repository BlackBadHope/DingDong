using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	private static void DrawDialogueBubbles(NPC npc, int numLines)
	{
		List<DingDongDialogueBubble> bubbles = new(DialogueBubbleLayout.MaxBubbleCount);
		if (NpcQuestSystem.TryGetQuestBubbleText(npc, Main.LocalPlayer, out string questText))
			bubbles.Add(new DingDongDialogueBubble(questText, DingDongDialogueBubbleAction.Quest));
		if (NpcQuestSystem.TryGetEscortBubbleText(npc, Main.LocalPlayer, out string escortText))
			bubbles.Add(new DingDongDialogueBubble(escortText, DingDongDialogueBubbleAction.Escort));
		if (IsServiceResident(npc.type))
			bubbles.Add(new DingDongDialogueBubble(Language.GetTextValue(HomeGoodsButtonKey), DingDongDialogueBubbleAction.HomeGoods));
		if (npc.type == NPCID.GoblinTinkerer)
			bubbles.Add(new DingDongDialogueBubble(Language.GetTextValue(GoblinFusionButtonKey), DingDongDialogueBubbleAction.GoblinFusion));
		if (bubbles.Count == 0)
			return;
		if (bubbles.Count > DialogueBubbleLayout.MaxBubbleCount)
			throw new InvalidOperationException($"NPC dialogue bubble row exceeded {DialogueBubbleLayout.MaxBubbleCount} actions.");
		float availableWidth = Math.Max(1f, Main.screenWidth - 48f);
		float[] measured = bubbles.Select(bubble => FontAssets.MouseText.Value.MeasureString(bubble.Text).X).ToArray();
		float[] widths = DialogueBubbleLayout.CalculateWidths(measured, availableWidth);
		float totalWidth = widths.Sum() + DialogueBubbleLayout.Gap * (widths.Length - 1);
		float startX = (Main.screenWidth - totalWidth) * 0.5f;
		float nativeButtonHeight = FontAssets.MouseText.Value.MeasureString("Ay").Y * 0.9f;
		float y = DialogueBubbleLayout.CalculateBubbleRowY(numLines, nativeButtonHeight, Main.screenHeight);
		for (int i = 0; i < bubbles.Count; i++)
		{
			DingDongDialogueBubble bubble = bubbles[i];
			Rectangle bounds = new((int)Math.Round(startX), (int)Math.Round(y), (int)Math.Round(widths[i]), (int)DialogueBubbleLayout.Height);
			bool hovered = bounds.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
			Color accent = BubbleAccent(bubble.Action);
			DrawBubblePanel(Main.spriteBatch, bounds, accent, hovered);
			string displayText = FitLabel(bubble.Text, bounds.Width - DialogueBubbleLayout.HorizontalPadding * 2f, out float textScale);
			Vector2 textSize = FontAssets.MouseText.Value.MeasureString(displayText) * textScale;
			Vector2 textPosition = new(bounds.Center.X - textSize.X * 0.5f, bounds.Center.Y - textSize.Y * 0.5f - 1f);
			Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, displayText, textPosition.X, textPosition.Y, hovered ? Color.White : DingDongUiTheme.Text, DingDongUiTheme.Shell, Vector2.Zero, textScale);
			if (hovered)
			{
				Main.LocalPlayer.mouseInterface = true;
				if (!string.Equals(displayText, bubble.Text, StringComparison.Ordinal))
					UICommon.TooltipMouseText(bubble.Text);
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{
					ActivateBubble(npc, bubble.Action);
					Main.mouseLeftRelease = false;
				}
			}

			startX += widths[i] + DialogueBubbleLayout.Gap;
		}
	}
}
