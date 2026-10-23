using System;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerActivation
{
	internal static bool IsActive => ContinentalWorldDimensions.IsContinental &&
		ModContent.GetInstance<DingDongServerConfig>().EnableContinentalWorlds &&
		string.Equals(Environment.GetEnvironmentVariable(
			"DINGDONG_EXPERIMENTAL_SCHEMATIC_WORLDGEN"), "1", StringComparison.Ordinal);
}
