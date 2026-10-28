using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace DingDongBiomeHomes.Worldgen;

// 28.10 (owner decision): every new Ding Dong world is a For the Worthy world. Vanilla runs
// ProcessSpecialWorldSeeds from the creation menu (ProcessSeed) and from the dedicated server's
// world prompt, so forcing it here covers both. S12 28.10: the seed text itself is also fixed to
// "ForTheWorthy" (shown in world info); vanilla GenerateWorld still replaces the numeric seed of a
// secret-seed world with a random one, so every world keeps a different landscape.
internal static partial class ContinentalWorldHooks
{
	internal const string ForcedSecretSeed = "for the worthy";
	internal const string ForcedSeedText = "ForTheWorthy";
	private static readonly FieldInfo SeedPlateField = PrivateField("_seedPlate");
	private static readonly FieldInfo OptionSeedField = PrivateField("_optionSeed");

	private static void InstallSeedLock() =>
		On_UIWorldCreation.ProcessSpecialWorldSeeds += ForceForTheWorthy;

	private static void UninstallSeedLock() =>
		On_UIWorldCreation.ProcessSpecialWorldSeeds -= ForceForTheWorthy;

	private static void ForceForTheWorthy(On_UIWorldCreation.orig_ProcessSpecialWorldSeeds orig, string processedSeed)
	{
		orig(ForcedSecretSeed);
		// The dedicated server has already called SetSeed with its own text; the client sets
		// _optionSeed before vanilla reaches SetSeed (see FinishCreatingWorld).
		if (Main.dedServ)
			Main.ActiveWorldFileData?.SetSeed(ForcedSeedText);
		if (!string.Equals(processedSeed?.Trim(), ForcedSecretSeed, StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(processedSeed?.Trim(), ForcedSeedText, StringComparison.OrdinalIgnoreCase))
			DingDongBiomeHomes.Instance?.Logger.Info($"DINGDONG_SEED_LOCK|requested={(string.IsNullOrWhiteSpace(processedSeed) ? "random" : "custom")}|applied=ForTheWorthy");
	}

	internal static void ForceSeedText(UIWorldCreation screen) => OptionSeedField.SetValue(screen, ForcedSeedText);

	// The seed row (randomize button + seed plate) is replaced by a fixed, non-editable label.
	private static void LockSeedRow(UIWorldCreation screen)
	{
		if (SeedPlateField.GetValue(screen) is not UIElement plate || plate.Parent is not UIElement parent)
			throw new InvalidOperationException("Vanilla world-seed menu contract changed.");
		StyleDimension top = plate.Top;
		var row = new List<UIElement>();
		foreach (UIElement child in parent.Children)
			if (child.Top.Pixels == top.Pixels && child.Top.Percent == top.Percent)
				row.Add(child);
		foreach (UIElement child in row)
			parent.RemoveChild(child);

		var label = new UIPanel
		{
			Top = top,
			Left = StyleDimension.FromPixels(0f),
			Width = StyleDimension.FromPercent(1f),
			Height = StyleDimension.FromPixels(40f),
			BackgroundColor = new Color(52, 34, 70) * 0.9f,
			BorderColor = new Color(255, 190, 70)
		};
		label.SetPadding(0f);
		label.Append(new UIText(Language.GetText($"{MenuKey}.SeedLocked.Name"), 0.95f)
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextColor = new Color(255, 214, 120)
		});
		string description = Language.GetTextValue($"{MenuKey}.SeedLocked.Description");
		label.OnMouseOver += (_, _) => SetDescription(screen, description);
		label.OnMouseOut += (_, _) => SetDescription(screen, string.Empty);
		parent.Append(label);
	}
}
