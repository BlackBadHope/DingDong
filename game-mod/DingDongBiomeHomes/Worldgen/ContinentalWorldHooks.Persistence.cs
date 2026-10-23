using System.IO;
using Terraria.IO;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static void LoadHeader(On_WorldFile.orig_LoadHeader orig, BinaryReader reader)
	{
		orig(reader);
		ContinentalWorldCapacity.Ensure("world-load-header");
	}
}
