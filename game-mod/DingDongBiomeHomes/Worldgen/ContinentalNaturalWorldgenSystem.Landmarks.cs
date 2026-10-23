using System;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalNaturalWorldgenSystem
{
	private void PlaceLandmarks(GenerationProgress progress,
		GameConfiguration configuration)
	{
		progress.Message = "Placing admitted continental landmarks";
		if (survey == null || !survey.IsComplete)
			throw new InvalidOperationException(
				"Continental landmark pass requires the accepted biome receipt.");
		ContinentalDungeonExpansionResult dungeon = ContinentalDungeonAnnex.Place();
		ContinentalAmazonTreeResult trees = ContinentalAmazonTrees.Place(survey);
		ContinentalNaturalWorldgenReceipt.RecordLandmarks(trees, dungeon);
		Mod.Logger.Info($"DINGDONG_CONTINENTAL_LANDMARKS|verdict=PASS|phase=worldgen-pass|amazonTrees={trees.Placed}/{trees.Target}|treeAttempts={trees.Attempts}/{trees.AttemptBudget}|dungeonRooms={dungeon.Rooms}|dungeonPortals={dungeon.Portals}|protected={dungeon.ProtectedAreas}|target={dungeon.TargetRooms}|attempts={dungeon.Attempts}|rerunPasses=0");
	}

	private void WriteReadOnlyReceipt(GenerationProgress progress,
		GameConfiguration configuration)
	{
		progress.Message = "Verifying continental natural-world receipt";
		if (!ContinentalNaturalWorldgenReceipt.TryRead(
			out ContinentalBiomeShapeReceipt biomes,
			out ContinentalAmazonTreeResult trees,
			out ContinentalDungeonExpansionResult dungeon))
			throw new InvalidOperationException(
				"Continental natural-world final receipt is missing.");
		ContinentalGenPassProfiler.WriteSummary(message => Mod.Logger.Info(message));
		Mod.Logger.Info($"DINGDONG_CONTINENTAL_RECEIPT|verdict=PASS|readOnly=True|{biomes.Compact()}|amazonTrees={trees.Placed}/{trees.Target}|treeAttempts={trees.Attempts}/{trees.AttemptBudget}|dungeonRooms={dungeon.Rooms}/{dungeon.TargetRooms}|dungeonPortals={dungeon.Portals}|protected={dungeon.ProtectedAreas}|areaDeficit={dungeon.AreaDeficit:F3}");
	}
}
