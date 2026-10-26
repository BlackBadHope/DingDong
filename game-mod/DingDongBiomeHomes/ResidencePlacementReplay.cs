using System;
using System.IO;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;

// Explicit diagnostic only; callers restore the pre-residence StructureMap.
// Operates on a test-world COPY, never a regular loaded game.
internal static class ResidencePlacementReplay
{
	internal static void Run(Mod mod)
	{
		if (!Main.dedServ ||
			Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Residence replay requires a dedicated diagnostic world copy.");
		if (SettlementWorldState.StructuresBuilt != 0)
			throw new InvalidOperationException("Refusing duplicate residence placement.");
		if (OceanExpeditions.OceanExpeditionWorldSystem.Layout is { } ocean)
		{
			string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "ResidenceReplay");
			Directory.CreateDirectory(directory);
			Terraria.Map.MapHelper.Initialize();
			Worldgen.NativeTerrainRaster.ExportRegion(Path.Combine(directory, "ocean-surface-before.bmp"),
				new(ocean.Left ? 0 : Main.maxTilesX - ocean.Width, Math.Max(0, ocean.SeaLevel - 500), ocean.Width, 900), 2);
		}
		bool previous = WorldGen.gen;
		try
		{
			WorldGen.gen = true;
			ModContent.GetInstance<BiomeHomeWorldgenSystem>().RunWorldCompilerPass(
				new GenerationProgress(), new GameConfiguration(new Newtonsoft.Json.Linq.JObject()));
		}
		finally { WorldGen.gen = previous; }
		if (SettlementWorldState.StructuresBuilt != 61 ||
			SettlementWorldState.HousingValid != 61 ||
			!SettlementWorldState.HasCompleteResidenceRegistry)
			throw new InvalidOperationException("Residence replay did not produce 61 registered, habitable homes.");
		ResidenceAcceptanceProbe.Run(mod);
		Worldgen.NativeTerrainRaster.Export();
		WorldFile.SaveWorld(useCloudSaving: false, resetTime: false);
		mod.Logger.Info("DINGDONG_RESIDENCE_PLACEMENT_REPLAY|verdict=PASS|structures=61|housing=61|worldMutated=True|saved=True|scope=diagnostic-copy");
	}
}
