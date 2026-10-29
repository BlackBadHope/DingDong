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

	// Residence transit (ResidenceTransitBuilder) places the hellevator beside the sanctuary.
	internal (Rectangle SafeArea, int SpawnX, int FloorY)? PlannedSanctuary =>
		generatedSanctuary == null ? null : (generatedSanctuary.SafeArea, generatedSanctuary.SpawnX, generatedSanctuary.FloorY);

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
		int lavaToWater = ClearSpawnLava(generatedSanctuary);
		int cleared = DrainAndBuild(generatedSanctuary, clearSpawnPocket: true);
		int unsafeLiquids = CountLiquidCells(generatedSanctuary.SafeArea);
		if (unsafeLiquids != 0 || !IsSpawnPocketSafe(generatedSanctuary.SpawnX,
			generatedSanctuary.FloorY))
			throw new InvalidOperationException(
				$"DingDong spawn sanctuary final gate failed: unsafeLiquids={unsafeLiquids}; spawn={generatedSanctuary.SpawnX},{generatedSanctuary.SpawnY}.");
		Main.spawnTileX = generatedSanctuary.SpawnX;
		Main.spawnTileY = generatedSanctuary.SpawnY;
		Mod.Logger.Info($"DINGDONG_SPAWN_SANCTUARY|verdict=PASS|phase=central-post-worldgen|variant={generatedSanctuary.VariantId}|source=StructureHelper/Structify@{AuthoredPresetCatalog.StructifyCommit}|license={AuthoredPresetCatalog.StructifyLicense}|area={generatedSanctuary.AssetArea.X},{generatedSanctuary.AssetArea.Y},{generatedSanctuary.AssetArea.Width},{generatedSanctuary.AssetArea.Height}|spawn={Main.spawnTileX},{Main.spawnTileY}|floor={generatedSanctuary.FloorY}|liquidCellsCleared={generatedSanctuary.LiquidCellsCleared + cleared}|unsafeLiquidsAfter=0|compactFoundation=True|visibleWidth={generatedSanctuary.SafeArea.Width}|sillHeight={SillHeight}|tallSideWalls=False|spawnLavaToWater={lavaToWater}");
	}

	// 29.10 (owner): no lava near spawn. For the Worthy leaves surface lava pools around the
	// sanctuary; inside a wide zone around spawn every lava cell becomes water. Where the zone
	// meets lava outside it, the two liquids harden into obsidian on the first settle, so the
	// outside lava cannot flow back in.
	internal const int SpawnLavaHalfWidth = 260, SpawnLavaAbove = 160, SpawnLavaBelow = 140;
	private static int ClearSpawnLava(SpawnSanctuaryReceipt plan)
	{
		int converted = 0;
		int left = Math.Max(1, plan.SpawnX - SpawnLavaHalfWidth), right = Math.Min(Main.maxTilesX - 2, plan.SpawnX + SpawnLavaHalfWidth);
		int top = Math.Max(1, plan.FloorY - SpawnLavaAbove), bottom = Math.Min(Main.maxTilesY - 2, plan.FloorY + SpawnLavaBelow);
		for (int x = left; x <= right; x++)
		for (int y = top; y <= bottom; y++)
		{
			Tile t = Main.tile[x, y];
			if (t.LiquidAmount == 0 || t.LiquidType != LiquidID.Lava) continue;
			t.LiquidType = LiquidID.Water;
			converted++;
		}
		return converted;
	}
}
