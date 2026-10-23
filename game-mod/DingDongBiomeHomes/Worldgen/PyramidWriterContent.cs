namespace DingDongBiomeHomes.Worldgen;

// Exact ModContent.TileType<T>/WallType<T> references in the installed
// SOTS.WorldgenHelpers.PyramidWorldgenHelper writer (geometry-r322 decompile).
// Do not replace this with a fuzzy name match: furnishings, keystones and true
// sandstone are authored pyramid content even without "Pyramid" in their name.
internal static class PyramidWriterContent
{
	internal static readonly string[] Tiles =
	{
		"AncientGoldCandelabraTile", "AncientGoldDresserTile", "AncientGoldGateTile",
		"AncientGoldPlatformTile", "AncientGoldSpikeTile", "AncientGoldTableTile",
		"AncientGoldThroneTile", "AncientGoldWorkbenchTile", "CrystalStatue",
		"CursedGrowthTile", "CursedHive", "CursedTumorTile", "EnchantedPickShrineTile",
		"EnchantedSwordShrineTile", "ManaStatue", "PyramidAmbientTile1x1",
		"PyramidAmbientTile1x1Curse", "PyramidAmbientTile2x1", "PyramidAmbientTile2x1Curse",
		"PyramidAmbientTile2x2", "PyramidAmbientTile3x1Curse", "PyramidAmbientTile3x2Curse",
		"PyramidBrickTile", "PyramidChestTile", "PyramidCrateTile", "PyramidGateTile",
		"PyramidPots", "PyramidRubbleTile", "PyramidSlabTile", "RefractingCrystalBlockTile",
		"RoyalGoldBrickTile", "RubyKeystoneTile", "RuinedPyramidBrickTile",
		"SarcophagusTile", "TrueSandstoneTile"
	};
	internal static readonly string[] Walls =
	{
		"AncientGoldBeamWall", "AncientGoldBrickWallTile", "PyramidBrickWallWall",
		"PyramidWallWall", "TrueSandstoneWallWall", "UnsafeCursedTumorWallWall",
		"UnsafePyramidBrickWallWall", "UnsafePyramidWallWall"
	};
}
