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

internal static partial class ContinentalWorldHooks
{
	private const string MenuKey = "Mods.DingDongBiomeHomes.WorldCreation";
	private static readonly FieldInfo SizeButtonsField = PrivateField("_sizeButtons");
	private static readonly FieldInfo OptionSizeField = PrivateField("_optionSize");
	private static readonly FieldInfo DescriptionTextField = PrivateField("_descriptionText");
	private static readonly MethodInfo UpdatePreviewMethod = PrivateMethod("UpdatePreviewPlate");
	// Presets 28.10: the preset chosen in the creation menu (null = none) and its buttons.
	private static ContinentalWorldPreset requestedPreset;
	private static readonly List<GroupOptionButton<int>> presetButtons = new();

	private static void InstallWorldCreationMenu()
	{
		InstallSeedLock();
		if (!Main.dedServ)
		{
			Terraria.GameContent.UI.States.On_UIWorldCreation.ctor += CreateWorldCreationMenu;
			On_UIWorldCreation.FinishCreatingWorld += FinishCreatingWorld;
		}
	}

	private static void UninstallWorldCreationMenu()
	{
		UninstallSeedLock();
		if (!Main.dedServ)
		{
			Terraria.GameContent.UI.States.On_UIWorldCreation.ctor -= CreateWorldCreationMenu;
			On_UIWorldCreation.FinishCreatingWorld -= FinishCreatingWorld;
		}
		ClearWorldCreationRequest();
	}

	private static void CreateWorldCreationMenu(
		Terraria.GameContent.UI.States.On_UIWorldCreation.orig_ctor orig,
		UIWorldCreation self)
	{
		orig(self);
		ClearWorldCreationRequest();
		LockSeedRow(self);
		// The Legendary difficulty is also forced at creation time; a changed vanilla
		// menu only loses the label, never the menu.
		try { LockDifficultyRow(self); }
		catch (Exception ex) { DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_DIFFICULTY_ROW|verdict=SKIPPED|reason=" + ex.Message); }
		if (IsEnabled())
			ReplaceSizeButtonsWithPresets(self);
	}

	// Presets 28.10 (design step 6): the three vanilla size buttons are removed and replaced by
	// one button per available preset; vanilla Large stays the hidden carrier of the selection.
	private static void ReplaceSizeButtonsWithPresets(UIWorldCreation screen)
	{
		Array rawButtons = (Array)SizeButtonsField.GetValue(screen);
		if (rawButtons?.Length != 3)
			throw new InvalidOperationException("Vanilla world-size menu contract changed.");

		UIElement[] buttons = new UIElement[rawButtons.Length];
		for (int index = 0; index < rawButtons.Length; index++)
			buttons[index] = (UIElement)rawButtons.GetValue(index);
		UIElement parent = buttons[0].Parent ??
			throw new InvalidOperationException("World-size menu has no parent.");

		IReadOnlyList<ContinentalWorldPreset> presets = ContinentalWorldDimensions.Available;
		if (presets.Count == 0)
			return; // Nothing released: keep the vanilla menu untouched.
		foreach (UIElement button in buttons)
			parent.RemoveChild(button);

		long? physical = MenuTotalPhysicalBytes();
		ContinentalWorldPreset recommended = ContinentalWorldDimensions.Recommend(physical);
		presetButtons.Clear();
		for (int index = 0; index < presets.Count; index++)
		{
			ContinentalWorldPreset preset = presets[index];
			int optionIndex = index;
			string description = PresetDescription(preset, preset == recommended, physical);
			var button = new GroupOptionButton<int>(optionIndex,
				Language.GetText(preset.LocKey + ".Name"),
				Language.GetText(preset.LocKey + ".Description"),
				new Color(255, 190, 70), PresetIcon(preset),
				1f, 1f, 16f);
			button.Top = buttons[0].Top;
			button.SetCurrentOption(-1);
			button.SetSnapPoint("size", index);
			button.OnLeftMouseDown += (_, _) => SelectPreset(screen, preset, optionIndex);
			button.OnMouseOver += (_, _) => SetDescription(screen, description);
			button.OnMouseOut += (_, _) => SetDescription(screen, string.Empty);
			LayoutSizeButton(button, index, presets.Count);
			parent.Append(button);
			presetButtons.Add(button);
		}
		// Without a selection "Create" would make a vanilla Medium world: open with the
		// recommended preset already chosen.
		int selected = Math.Max(0, IndexOf(presets, recommended));
		SelectPreset(screen, presets[selected], selected);
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_CREATION_MENU|presets={presets.Count}|ids={string.Join(",", IdsOf(presets))}" +
			$"|selected={presets[selected].Id}|preview={ContinentalWorldDimensions.PreviewEnabled}" +
			$"|physicalGiB={(physical is long bytes ? (bytes / (1024d * 1024d * 1024d)).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "unknown")}");
	}

	private static void SelectPreset(UIWorldCreation screen, ContinentalWorldPreset preset, int optionIndex)
	{
		requestedPreset = preset;
		// Presets 28.10: vanilla Large is the carrier for FinishCreatingWorld.
		OptionSizeField.SetValue(screen, Enum.ToObject(OptionSizeField.FieldType, 2));
		foreach (GroupOptionButton<int> button in presetButtons)
			button.SetCurrentOption(optionIndex);
		UpdatePreviewMethod.Invoke(screen, null);
	}

	// Presets 28.10: hover text = localized description + recommendation / RAM warning /
	// "residences not guaranteed" lines.
	private static string PresetDescription(ContinentalWorldPreset preset, bool recommended, long? physical)
	{
		string text = Language.GetTextValue(preset.LocKey + ".Description");
		const string NewLine = "\n";
		if (recommended && ContinentalWorldDimensions.Available.Count > 1)
			text += NewLine + Language.GetTextValue($"{MenuKey}.PresetRecommended");
		if (physical is long bytes && !ContinentalWorldDimensions.FitsPhysical(preset, bytes))
			text += NewLine + Language.GetTextValue($"{MenuKey}.PresetAboveRam", preset.RamTierGb,
				(bytes / (1024d * 1024d * 1024d)).ToString("F1"));
		if (!preset.ResidencesProven)
			text += NewLine + Language.GetTextValue($"{MenuKey}.PresetResidencesUnproven");
		return text;
	}

	private static string PresetIcon(ContinentalWorldPreset preset) => preset.RamTierGb switch
	{
		<= 12 => "Images/UI/WorldCreation/IconSizeSmall",
		<= 16 => "Images/UI/WorldCreation/IconSizeMedium",
		_ => "Images/UI/WorldCreation/IconSizeLarge"
	};

	private static long? MenuTotalPhysicalBytes()
	{
		try { return ContinentalMemoryStatus.Read().TotalPhysicalBytes; }
		catch (Exception ex) when (ex is PlatformNotSupportedException or
			System.ComponentModel.Win32Exception or ContinentalMemoryAdmissionException)
		{
			return null;
		}
	}

	private static int IndexOf(IReadOnlyList<ContinentalWorldPreset> presets, ContinentalWorldPreset preset)
	{
		for (int index = 0; index < presets.Count; index++)
			if (presets[index] == preset) return index;
		return -1;
	}

	private static IEnumerable<string> IdsOf(IReadOnlyList<ContinentalWorldPreset> presets)
	{
		foreach (ContinentalWorldPreset preset in presets)
			yield return preset.Id;
	}

	private static void LayoutSizeButton(UIElement button, int index, int count)
	{
		button.Width = StyleDimension.FromPixelsAndPercent(-4f * (count - 1), 1f / count);
		button.Left = StyleDimension.FromPercent(0f);
		// Presets 28.10: a single preset (preview off) spans the row; avoid 0/0.
		button.HAlign = count > 1 ? index / (float)(count - 1) : 0.5f;
	}

	private static void SetDescription(UIWorldCreation screen, string text) =>
		((UIText)DescriptionTextField.GetValue(screen))?.SetText(text);

	private static void ClearWorldCreationRequest()
	{
		requestedPreset = null;
		foreach (GroupOptionButton<int> button in presetButtons)
			button.SetCurrentOption(-1);
	}

	private static FieldInfo PrivateField(string name) =>
		typeof(UIWorldCreation).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
		throw new MissingFieldException(typeof(UIWorldCreation).FullName, name);

	private static MethodInfo PrivateMethod(string name) =>
		typeof(UIWorldCreation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
		throw new MissingMethodException(typeof(UIWorldCreation).FullName, name);
}
