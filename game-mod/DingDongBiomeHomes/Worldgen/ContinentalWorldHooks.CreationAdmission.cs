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
		if (ShouldPromoteLargeWorld() &&
			!ContinentalWorldCapacity.TryAdmitClientCreation(out string error))
		{
			Utils.ShowFancyErrorMessage(
				Language.GetTextValue($"{MenuKey}.MemoryDenied", error), 888, self);
			return;
		}
		orig(self);
	}
}
