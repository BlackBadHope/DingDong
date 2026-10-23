using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;

public sealed partial class SpawnSanctuaryWorldgenSystem
{
	public override void PreWorldGen()
	{
		generatedSanctuary = null;
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks,
		ref double totalWeight)
	{
		if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive) return;
		tasks.Add(new PassLegacy("DingDong lava-proof spawn sanctuary",
			GenerateSanctuary, PassWeight));
		totalWeight += PassWeight;
	}

	// ContinentalSaveBudgetSystem is the only PostWorldGen owner. It invokes this
	// first, then performs the final exact tile scan without a later DingDong write.
	internal void FinalizeBeforeSave()
	{
		if (generatedSanctuary == null)
			throw new InvalidOperationException(
				"DingDong spawn sanctuary pass did not run.");
		int cleared = DrainAndBuild(generatedSanctuary, clearSpawnPocket: true);
		int unsafeLiquids = CountLiquidCells(generatedSanctuary.SafeArea);
		if (unsafeLiquids != 0 || !IsSpawnPocketSafe(generatedSanctuary.SpawnX,
			generatedSanctuary.FloorY))
			throw new InvalidOperationException(
				$"DingDong spawn sanctuary final gate failed: unsafeLiquids={unsafeLiquids}; spawn={generatedSanctuary.SpawnX},{generatedSanctuary.SpawnY}.");
		Main.spawnTileX = generatedSanctuary.SpawnX;
		Main.spawnTileY = generatedSanctuary.SpawnY;
		Mod.Logger.Info($"DINGDONG_SPAWN_SANCTUARY|verdict=PASS|phase=central-post-worldgen|variant={generatedSanctuary.VariantId}|source=StructureHelper/Structify@{AuthoredPresetCatalog.StructifyCommit}|license={AuthoredPresetCatalog.StructifyLicense}|area={generatedSanctuary.AssetArea.X},{generatedSanctuary.AssetArea.Y},{generatedSanctuary.AssetArea.Width},{generatedSanctuary.AssetArea.Height}|spawn={Main.spawnTileX},{Main.spawnTileY}|floor={generatedSanctuary.FloorY}|liquidCellsCleared={generatedSanctuary.LiquidCellsCleared + cleared}|unsafeLiquidsAfter=0|compactFoundation=True|visibleWidth={generatedSanctuary.SafeArea.Width}|sillHeight={SillHeight}|tallSideWalls=False");
	}
}
