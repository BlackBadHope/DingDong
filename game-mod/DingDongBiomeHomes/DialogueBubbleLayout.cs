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
internal static class DialogueBubbleLayout
{
	internal const float Height = 44f;
	internal const float Gap = 8f;
	internal const float HorizontalPadding = 18f;
	internal const float MinWidth = 132f;
	internal const float MaxWidth = 280f;
	internal const float MinTextScale = 0.78f;
	internal const float MaxTextScale = 0.86f;
	internal const float NativeButtonBaseY = 130f;
	internal const float NativeButtonLineStride = 30f;
	internal const float NativeRowGap = 8f;
	internal const int MaxBubbleCount = 3;
	internal static float[] CalculateWidths(IReadOnlyList<float> measuredTextWidths, float availableWidth)
	{
		float[] widths = new float[measuredTextWidths.Count];
		if (widths.Length == 0)
			return widths;
		float gapWidth = Gap * (widths.Length - 1);
		float widthBudget = Math.Max(0f, availableWidth - gapWidth);
		float desiredTotal = 0f;
		for (int i = 0; i < widths.Length; i++)
		{
			widths[i] = MathHelper.Clamp(measuredTextWidths[i] * MaxTextScale + HorizontalPadding * 2f, MinWidth, MaxWidth);
			desiredTotal += widths[i];
		}

		if (desiredTotal <= widthBudget)
			return widths;
		// Keep short actions readable and give longer labels the remaining space.
		// Equal-width compression wastes space in short buttons while truncating long ones.
		float minimum = Math.Min(MinWidth, widthBudget / widths.Length);
		float flexibleWidth = desiredTotal - minimum * widths.Length;
		float remainingWidth = widthBudget - minimum * widths.Length;
		float ratio = flexibleWidth > 0f ? MathHelper.Clamp(remainingWidth / flexibleWidth, 0f, 1f) : 0f;
		for (int i = 0; i < widths.Length; i++)
			widths[i] = minimum + (widths[i] - minimum) * ratio;
		return widths;
	}

	internal static float TextScale(float measuredTextWidth, float bubbleWidth)
	{
		if (measuredTextWidth <= 0f)
			return MaxTextScale;
		return MathHelper.Clamp((bubbleWidth - HorizontalPadding * 2f) / measuredTextWidth, MinTextScale, MaxTextScale);
	}

	internal static float NativeButtonOriginY(int numLines) => NativeButtonBaseY + Math.Max(0, numLines) * NativeButtonLineStride;
	internal static float CalculateBubbleRowY(int numLines, float nativeButtonHeight, float screenHeight)
	{
		float requested = NativeButtonOriginY(numLines) + Math.Max(0f, nativeButtonHeight) + NativeRowGap;
		return MathHelper.Clamp(requested, 12f, Math.Max(12f, screenHeight - Height - 12f));
	}
}
