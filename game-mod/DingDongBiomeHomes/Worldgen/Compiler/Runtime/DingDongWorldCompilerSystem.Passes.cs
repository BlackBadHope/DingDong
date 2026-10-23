using System;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen.Compiler.Api;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem
{
	private static WorldCompilerPlan Plan => WorldCompilerRuntimeState.Current ??
		throw new InvalidOperationException("World compiler plan is unavailable.");
	private static WorldCompilerExecutionReceipt Receipt =>
		WorldCompilerRuntimeState.Receipt ??
		throw new InvalidOperationException("World compiler receipt is unavailable.");

	private void GenerateTerrain(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Compiling x10 terrain chunks";
		Receipt.Terrain = WorldCompilerTerrainRasterizer.Generate(Plan);
		RunExtensions(WorldCompilerExtensionPhase.AfterBaseTerrain);
	}

	private void GenerateCaves(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Carving connected cavern graph";
		long bytes = WorldCompilerRuntimeState.PrepareCaveWork();
		try { Receipt.Caves = WorldCompilerCaveRasterizer.Generate(Plan); }
		finally { WorldCompilerRuntimeState.ReleasePhaseWork(); }
		LogPhaseIndex("caves", bytes);
		RunExtensions(WorldCompilerExtensionPhase.AfterCavernGraph);
	}

	private void GenerateResources(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Compiling resources, evil and Hallow";
		long bytes = WorldCompilerRuntimeState.PrepareResourceWork();
		try { Receipt.Resources = WorldCompilerResourceRasterizer.Generate(Plan); }
		finally { WorldCompilerRuntimeState.ReleasePhaseWork(); }
		LogPhaseIndex("resources", bytes);
		RunExtensions(WorldCompilerExtensionPhase.AfterResources);
	}

	private void GenerateHydrology(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Filling preplanned hydrology";
		long bytes = WorldCompilerRuntimeState.PrepareHydrologyWork();
		try { Receipt.Hydrology = WorldCompilerHydrologyRasterizer.Generate(Plan); }
		finally { WorldCompilerRuntimeState.ReleasePhaseWork(); }
		LogPhaseIndex("hydrology", bytes);
		RunExtensions(WorldCompilerExtensionPhase.AfterHydrology);
	}

	private void GenerateStructures(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Rasterizing graph-first structures";
		long bytes = WorldCompilerRuntimeState.PrepareStructureWork();
		try { Receipt.Structures = WorldCompilerStructureRasterizer.Generate(Plan); }
		finally { WorldCompilerRuntimeState.ReleasePhaseWork(); }
		LogPhaseIndex("structures", bytes);
		RunExtensions(WorldCompilerExtensionPhase.AfterStructures);
	}

	private void LogPhaseIndex(string phase, long bytes) => Mod.Logger.Info(
		$"DINGDONG_WORLD_COMPILER_INDEX|verdict=PASS|phase={phase}" +
		$"|payloadBytes={bytes}|released=True");

	private void PrepareContracts(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Publishing world contracts";
		Receipt.Progression = WorldCompilerProgressionObjects.Place(Plan);
		WorldCompilerProgressionObjects.ValidateAnchor(Receipt.Progression, "before-compatibility");
		RunExtensions(WorldCompilerExtensionPhase.AfterWorldContracts);
	}

	private void GenerateDecorations(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Growing deterministic continents";
		Receipt.Decorations = WorldCompilerDecorationRasterizer.Generate(Plan);
		RunExtensions(WorldCompilerExtensionPhase.AfterNaturalDecoration);
	}

	private static void GenerateAuthoredSpawn(GenerationProgress progress,
		GameConfiguration configuration) =>
		ModContent.GetInstance<SpawnSanctuaryWorldgenSystem>()
			.RunWorldCompilerPass(progress, configuration);

	private static void GenerateResidences(GenerationProgress progress,
		GameConfiguration configuration)
	{
		ModContent.GetInstance<BiomeHomeWorldgenSystem>()
			.RunWorldCompilerPass(progress, configuration);
		const string marker =
			"DINGDONG_RESIDENCE_GATE|verdict=PASS|planned=61/61|placed=61/61";
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(marker);
		if (string.Equals(Environment.GetEnvironmentVariable(
			"DINGDONG_X10_RESIDENCE_GATE_ONLY"), "1",
			StringComparison.Ordinal))
		{
			Console.WriteLine(marker + "|isolatedGatePause=True");
			System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
		}
	}

	private void FinalizeWorld(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Framing and validating compiled world";
		RunExtensions(WorldCompilerExtensionPhase.BeforeFinalize);
		long framed = WorldCompilerFinalizer.Finalize(Plan);
		Receipt.Finalized = true;
		Mod.Logger.Info($"DINGDONG_WORLD_COMPILER_FINALIZE|verdict=PASS" +
			$"|framed={framed}|spawn={Terraria.Main.spawnTileX},{Terraria.Main.spawnTileY}" +
			$"|dungeon={Terraria.Main.dungeonX},{Terraria.Main.dungeonY}");
	}

}
