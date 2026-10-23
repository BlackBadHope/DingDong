using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private static void LogCapacity(string reason)
	{
		int expectedX = (Main.maxTilesX - 1) / Main.sectionWidth + 1;
		int expectedY = (Main.maxTilesY - 1) / Main.sectionHeight + 1;
		// LoadHeader -> clearWorld sets dimensions before setWorldSize updates
		// section counts. That pre-callback allocation is not the final receipt.
		string verdict = Main.maxSectionsX == expectedX && Main.maxSectionsY == expectedY
			? "PASS" : "PREPARED";
		DingDongBiomeHomes.Instance.Logger.Info(
			$"CONTINENTAL_CAPACITY={verdict} reason={reason}" +
			$" world={Main.maxTilesX}x{Main.maxTilesY}" +
			$" storage={Main.tile.Width}x{Main.tile.Height}" +
			$" sections={Main.maxSectionsX}x{Main.maxSectionsY}" +
			$" tileAllocations={tileAllocations} mapAllocations={mapAllocations}" +
			$" clientMapReady={HasClientMapCapacity()} mapDeferred={clientMapDeferred}" +
			$" thread={System.Environment.CurrentManagedThreadId}");
	}
}
