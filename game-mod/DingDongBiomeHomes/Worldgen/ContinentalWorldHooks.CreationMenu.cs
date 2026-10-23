using System;
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
	private static bool continentalWorldRequested;
	private static GroupOptionButton<bool> continentalButton;

	private static void InstallWorldCreationMenu()
	{
		if (!Main.dedServ)
		{
			Terraria.GameContent.UI.States.On_UIWorldCreation.ctor += CreateWorldCreationMenu;
			On_UIWorldCreation.FinishCreatingWorld += FinishCreatingWorld;
		}
	}

	private static void UninstallWorldCreationMenu()
	{
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
		if (IsEnabled())
			AppendContinentalSizeButton(self);
	}

	private static void AppendContinentalSizeButton(UIWorldCreation screen)
	{
		Array rawButtons = (Array)SizeButtonsField.GetValue(screen);
		if (rawButtons?.Length != 3)
			throw new InvalidOperationException("Vanilla world-size menu contract changed.");

		UIElement[] buttons = new UIElement[rawButtons.Length];
		for (int index = 0; index < rawButtons.Length; index++)
			buttons[index] = (UIElement)rawButtons.GetValue(index);
		UIElement parent = buttons[0].Parent ??
			throw new InvalidOperationException("World-size menu has no parent.");

		continentalButton = new GroupOptionButton<bool>(true,
			Language.GetText($"{MenuKey}.Name"),
			Language.GetText($"{MenuKey}.Description"),
			new Color(255, 190, 70), "Images/UI/WorldCreation/IconSizeLarge",
			1f, 1f, 16f);
		continentalButton.Top = buttons[0].Top;
		continentalButton.SetCurrentOption(false);
		continentalButton.SetSnapPoint("size", 3);
		continentalButton.OnLeftMouseDown += (_, _) => SelectContinentalWorld(screen, buttons);
		continentalButton.OnMouseOver += (_, _) => SetDescription(screen,
			Language.GetTextValue($"{MenuKey}.Description"));
		continentalButton.OnMouseOut += (_, _) => SetDescription(screen, string.Empty);

		for (int index = 0; index < buttons.Length; index++)
		{
			LayoutSizeButton(buttons[index], index, 4);
			buttons[index].OnLeftMouseDown += (_, _) => ClearWorldCreationRequest();
		}
		LayoutSizeButton(continentalButton, 3, 4);
		parent.Append(continentalButton);
	}

	private static void SelectContinentalWorld(UIWorldCreation screen, UIElement[] vanillaButtons)
	{
		continentalWorldRequested = true;
		Type sizeType = OptionSizeField.FieldType;
		OptionSizeField.SetValue(screen, Enum.ToObject(sizeType, 2));
		object noVanillaSelection = Enum.ToObject(sizeType, 3);
		foreach (UIElement button in vanillaButtons)
			button.GetType().GetMethod("SetCurrentOption")?.Invoke(button,
				new[] { noVanillaSelection });
		continentalButton.SetCurrentOption(true);
		UpdatePreviewMethod.Invoke(screen, null);
	}

	private static void LayoutSizeButton(UIElement button, int index, int count)
	{
		button.Width = StyleDimension.FromPixelsAndPercent(-4f * (count - 1), 1f / count);
		button.Left = StyleDimension.FromPercent(0f);
		button.HAlign = index / (float)(count - 1);
	}

	private static void SetDescription(UIWorldCreation screen, string text) =>
		((UIText)DescriptionTextField.GetValue(screen))?.SetText(text);

	private static void ClearWorldCreationRequest()
	{
		continentalWorldRequested = false;
		continentalButton?.SetCurrentOption(false);
	}

	private static FieldInfo PrivateField(string name) =>
		typeof(UIWorldCreation).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
		throw new MissingFieldException(typeof(UIWorldCreation).FullName, name);

	private static MethodInfo PrivateMethod(string name) =>
		typeof(UIWorldCreation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
		throw new MissingMethodException(typeof(UIWorldCreation).FullName, name);
}
