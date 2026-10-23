using System;
using System.Linq;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class NpcDialogueCompatibilityContract
{
	internal static bool Pass(out string failure)
	{
		failure = string.Empty;
		if (NpcTownManifest.AuthoredCount != 61)
		{
			failure = $"resident-count:{NpcTownManifest.AuthoredCount}";
			return false;
		}

		(float[] Widths, float Available)[] measuredScenarios =
		{
			(new[] { 96f }, 520f),
			(new[] { 250f, 180f }, 360f),
			(new[] { 640f, 520f }, 520f),
			(new[] { 640f, 520f, 480f }, 752f)
		};
		foreach ((float[] widthsToMeasure, float available) in measuredScenarios)
		{
			float[] widths = DialogueBubbleLayout.CalculateWidths(widthsToMeasure, available);
			float occupied = widths.Length == 0
				? 0f
				: widths.Sum() + DialogueBubbleLayout.Gap * (widths.Length - 1);
			if (occupied > available + 0.01f)
			{
				failure = $"row-overflow:{occupied:0.##}";
				return false;
			}
			for (int index = 0; index < widths.Length; index++)
			{
				if (widths[index] <= 0f || widths[index] > DialogueBubbleLayout.MaxWidth + 0.01f)
				{
					failure = $"width:{index}:{widths[index]:0.##}";
					return false;
				}
				float textScale = DialogueBubbleLayout.TextScale(widthsToMeasure[index], widths[index]);
				if (textScale < DialogueBubbleLayout.MinTextScale - 0.001f
					|| textScale > DialogueBubbleLayout.MaxTextScale + 0.001f)
				{
					failure = $"scale:{index}:{textScale:0.###}";
					return false;
				}
			}
		}
		if (Enum.GetValues<DingDongDialogueBubbleAction>().Length != 4
			|| DialogueBubbleLayout.MaxBubbleCount != 3)
		{
			failure = "action-count";
			return false;
		}
		float rowY = DialogueBubbleLayout.CalculateBubbleRowY(numLines: 4, nativeButtonHeight: 18f, screenHeight: 720f);
		if (Math.Abs(rowY - 276f) > 0.01f)
		{
			failure = $"native-row-anchor:{rowY:0.##}";
			return false;
		}

		return true;
	}
}

public sealed class NpcDialogueCompatibilityContractSystem : ModSystem
{
	public override void PostSetupContent()
	{
		bool pass = NpcDialogueCompatibilityContract.Pass(out string failure);
		string marker = $"DINGDONG_NPC_DIALOGUE_CONTRACT|verdict={(pass ? "PASS" : "FAIL")}|residents={NpcTownManifest.AuthoredCount}/61|actions=Quest,Escort,HomeGoods,Fusion|maxBubbles={DialogueBubbleLayout.MaxBubbleCount}|nativeAnchor=130+30n|layout=bounded|failure={(pass ? "none" : failure)}";
		if (pass)
			Mod.Logger.Info(marker);
		else
			Mod.Logger.Error(marker);
	}
}
