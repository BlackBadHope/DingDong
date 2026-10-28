using System;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerActivation
{
	// Presets 28.10: the experimental schematic compiler stays x10-only.
	internal static bool IsActive => ContinentalWorldDimensions.IsX10 && ContinentalWorldDimensions.IsContinental &&
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds &&
		string.Equals(Environment.GetEnvironmentVariable(
			"DINGDONG_EXPERIMENTAL_SCHEMATIC_WORLDGEN"), "1", StringComparison.Ordinal);
}
