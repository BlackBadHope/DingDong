using System;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalNaturalWorldgenSystem
{
	private void ShapeBiomes(GenerationProgress progress,
		GameConfiguration configuration)
	{
		progress.Message = "Shaping continental Sahara, Arctic and Amazon";
		survey = ContinentalSurfaceSurvey.Create();
		if (!survey.IsComplete)
		{
			Mod.Logger.Error($"DINGDONG_CONTINENTAL_BIOMES|verdict=WITHHELD|reason={survey.Failure}|mutated=0");
			throw new InvalidOperationException(
				$"Continental biome survey withheld: {survey.Failure}.");
		}
		ContinentalBiomeShapeReceipt receipt = ContinentalBiomeShaper.Shape(survey);
		if (!receipt.TryValidate(out string failure))
		{
			Mod.Logger.Error($"DINGDONG_CONTINENTAL_BIOMES|verdict=WITHHELD|reason={failure}|{receipt.Compact()}|mutated={receipt.TotalChanged}");
			throw new InvalidOperationException(
				$"Continental biome coverage withheld: {failure}.");
		}
		ContinentalNaturalWorldgenReceipt.RecordBiomes(receipt);
		Mod.Logger.Info($"DINGDONG_CONTINENTAL_BIOMES|verdict=PASS|{receipt.Compact()}|surveyTiles={survey.TilesInspected}|forest={survey.ForestReserve}|desert={survey.DesertTarget}:{ContinentalBiomeVolumePolicy.DesertDepth}|snow={survey.SnowTarget}:{ContinentalBiomeVolumePolicy.ArcticDepth}|jungle={survey.JungleTarget}:{ContinentalBiomeVolumePolicy.AmazonDepth}");
	}
}
