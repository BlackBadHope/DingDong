using System;
using System.Linq;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalCreationChoice
{
	internal const string ServerSizeVariable = "DINGDONG_SERVER_WORLD_SIZE";

	// Presets 28.10 (design step 5, owner decision B4): returns the preset that a new world is
	// promoted to, or null for no promotion.
	//  - client: the preset chosen in the creation menu;
	//  - dedicated server: DINGDONG_SERVER_WORLD_SIZE = ram12|ram16|ram24|ram32 (x10 is an alias of
	//    ram32). Presets that have not passed their server run need DINGDONG_PRESET_PREVIEW=1.
	//  - Presets 28.10 review fix: empty picks the preset by physical RAM like the menu, but only one
	//    that FITS physical RAM; RAM unknown or nothing fits -> no promotion (vanilla, as before),
	//    never a silent x10 on an 8-16 GB or non-Windows host. "large" and "vanilla" are explicit
	//    "no promotion" values: earlier denial/OOM texts and runbooks (RamOpt/MeasureRam.py) use
	//    DINGDONG_SERVER_WORLD_SIZE=large to get a vanilla Large world, and old builds accept it too.
	internal static ContinentalWorldPreset ResolveRequestedPreset(bool enabled, bool dedicated,
		ContinentalWorldPreset menuPreset, string serverSize, long? totalPhysicalBytes)
	{
		if (!enabled) return null;
		if (!dedicated) return menuPreset;
		string value = serverSize?.Trim();
		if (string.IsNullOrEmpty(value))
			return ContinentalWorldDimensions.RecommendFitting(totalPhysicalBytes);
		if (value.Equals("large", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("vanilla", StringComparison.OrdinalIgnoreCase))
			return null;
		if (value.Equals("x10", StringComparison.OrdinalIgnoreCase))
			return ContinentalWorldDimensions.X10;
		ContinentalWorldPreset preset = ContinentalWorldDimensions.FindById(value);
		if (preset == null || !preset.Offered)
			throw new ArgumentException($"{ServerSizeVariable} must be one of: large, vanilla, x10, " +
				string.Join(", ", ContinentalWorldDimensions.Presets.Where(p => p.Offered).Select(p => p.Id)) +
				$" (got '{value}').");
		if (!ContinentalWorldDimensions.IsAvailable(preset))
			throw new ArgumentException($"{ServerSizeVariable}={preset.Id}: this preset has not passed its server run yet; " +
				$"set {ContinentalWorldDimensions.PreviewVariable}=1 to test it, or use x10.");
		return preset;
	}
}
