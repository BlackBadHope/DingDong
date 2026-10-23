namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalNaturalWorldgenReceipt
{
	private static bool biomeComplete;
	private static bool landmarkComplete;
	private static ContinentalBiomeShapeReceipt biomes;
	private static ContinentalAmazonTreeResult amazonTrees;
	private static ContinentalDungeonExpansionResult dungeon;

	internal static void Reset()
	{
		biomeComplete = false;
		landmarkComplete = false;
		biomes = null;
		amazonTrees = default;
		dungeon = default;
	}

	internal static void RecordBiomes(ContinentalBiomeShapeReceipt result)
	{
		biomes = result;
		biomeComplete = true;
	}

	internal static void RecordLandmarks(ContinentalAmazonTreeResult trees,
		ContinentalDungeonExpansionResult result)
	{
		amazonTrees = trees;
		dungeon = result;
		landmarkComplete = true;
	}

	internal static bool TryRead(out ContinentalBiomeShapeReceipt biomeResult,
		out ContinentalAmazonTreeResult trees,
		out ContinentalDungeonExpansionResult dungeonResult)
	{
		biomeResult = biomes;
		trees = amazonTrees;
		dungeonResult = dungeon;
		return biomeComplete && landmarkComplete;
	}
}
