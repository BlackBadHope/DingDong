using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalWorldDimensions
{
	internal const int VanillaLargeWidth = 8400;
	internal const int VanillaLargeHeight = 2400;
	internal const int Width = 28000;
	internal const int Height = 7200;
	internal const int StorageWidth = Width + 1;
	internal const int StorageHeight = Height + 1;
	internal const long TileCount = (long)Width * Height;
	internal const long StorageCellCount = (long)StorageWidth * StorageHeight;

	internal static bool IsVanillaLarge =>
		Main.maxTilesX == VanillaLargeWidth && Main.maxTilesY == VanillaLargeHeight;

	internal static bool IsContinental => Main.maxTilesX == Width && Main.maxTilesY == Height;
	internal static bool IsDeploymentWorld => IsVanillaLarge || IsContinental;

	internal static bool SupportsAuthoredSettlements =>
		Main.maxTilesX >= VanillaLargeWidth && Main.maxTilesY >= VanillaLargeHeight;

	internal static void Validate()
	{
		long vanillaTiles = (long)VanillaLargeWidth * VanillaLargeHeight;
		if (TileCount != vanillaTiles * 10L)
			throw new InvalidOperationException("Continental dimensions must equal ten Large-world areas.");
		if (Width >= short.MaxValue || Height >= short.MaxValue ||
			StorageWidth > ushort.MaxValue || StorageHeight > ushort.MaxValue)
			throw new InvalidOperationException("Continental storage exceeds signed multiplayer coordinates.");
		if (StorageCellCount > int.MaxValue || StorageCellCount > Array.MaxLength)
			throw new InvalidOperationException("Continental Tilemap length exceeds CLR array indexing capacity.");
		if (Width % Main.sectionWidth != 0 || Height % Main.sectionHeight != 0)
			throw new InvalidOperationException("Continental dimensions must align to network sections.");
	}
}
