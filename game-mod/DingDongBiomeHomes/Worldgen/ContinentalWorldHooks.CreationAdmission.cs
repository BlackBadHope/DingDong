using System;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static void FinishCreatingWorld(
		On_UIWorldCreation.orig_FinishCreatingWorld orig, UIWorldCreation self)
	{
		// FinishCreatingWorld mutates dimensions, seed and metadata, then queues
		// the worker. Reject before calling any of it; keep the user's options.
		// Presets 28.10: admit the selected preset; the carrier size is re-asserted because
		// vanilla code may have reset _optionSize after the preset was picked.
		if (RequestedPreset() is ContinentalWorldPreset preset)
		{
			if (!ContinentalWorldCapacity.TryAdmitClientCreation(preset, out string error))
			{
				Utils.ShowFancyErrorMessage(
					Language.GetTextValue($"{MenuKey}.MemoryDenied", error,
						Language.GetTextValue(preset.LocKey + ".Name")), 888, self);
				return;
			}
			OptionSizeField.SetValue(self, Enum.ToObject(OptionSizeField.FieldType, 2));
		}
		orig(self);
	}
}
