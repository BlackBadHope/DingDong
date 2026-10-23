using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldHooks
{
	private static void GetData(
		On_MessageBuffer.orig_GetData orig,
		MessageBuffer self,
		int start,
		int length,
		out int messageType)
	{
		orig(self, start, length, out messageType);
		if (messageType != MessageID.WorldData)
			return;

		// Vanilla reads world dimensions from WorldData but does not recompute these
		// derived bounds. Custom worlds must do so before allocating section state.
		ContinentalWorldCapacity.StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		WorldGen.setWorldSize();
		// Joining clients need the tile planes before section streaming starts,
		// but the full map can be allocated after the world finishes loading.
		// This matches the creation path and avoids a needless 0.75 GiB peak at
		// the WorldData packet boundary.
		ContinentalWorldCapacity.Ensure("multiplayer-world-data",
			deferClientMap: true);
	}
}
