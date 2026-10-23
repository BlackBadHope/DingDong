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
/// <summary>
/// Adds one seed-selected authored Structure Helper house to the world spawn.
/// A compact stone foundation, low lava sills and a final PostWorldGen drain
/// keep FTW/other generator passes from leaving the player inside lava without
/// turning the whole visible spawn into an arena-sized black box.
/// </summary>
public sealed partial class SpawnSanctuaryWorldgenSystem
{
	internal void RunWorldCompilerPass(GenerationProgress progress,
		GameConfiguration configuration) => GenerateSanctuary(progress, configuration);

	private void GenerateSanctuary(GenerationProgress progress, GameConfiguration configuration)
	{
		progress.Message = "Building a lava-proof spawn sanctuary";
		string variantId = VariantIds[WorldGen.genRand.Next(VariantIds.Length)];
		if (!AuthoredPresetCatalog.TryGet(variantId, out AuthoredPreset preset))
			throw new InvalidOperationException($"DingDong spawn sanctuary preset is missing: {variantId}.");
		if (preset.ContainsNbt || preset.PlacementMode != AuthoredPlacementMode.SurfaceStructure || !preset.GroundOffset.HasValue)
			throw new InvalidOperationException($"DingDong spawn sanctuary preset is not a reviewed compact Structure Helper asset: {variantId}.");
		int foundationHalfWidth = Math.Clamp((preset.Width + 1) / 2 + FoundationMargin, MinimumFoundationHalfWidth, MaximumFoundationHalfWidth);
		int centerX = Math.Clamp(Main.spawnTileX, foundationHalfWidth + 30, Main.maxTilesX - foundationHalfWidth - 30);
		int floorY = FindSurfaceFloor(centerX);
		int originX = centerX - preset.Width / 2;
		int originY = floorY - (preset.Height - 1 - preset.GroundOffset.Value);
		originY = Math.Clamp(originY, 30, Main.maxTilesY - preset.Height - 30);
		Rectangle area = new(originX, originY, preset.Width, preset.Height);
		int safeTop = Math.Max(10, Math.Min(area.Top - 3, floorY - SpawnPocketHeight - 2));
		int safeBottom = Math.Min(Main.maxTilesY - 11, Math.Max(area.Bottom + 2, floorY + FoundationDepth));
		Rectangle safeArea = new(centerX - foundationHalfWidth, safeTop, foundationHalfWidth * 2 + 1, safeBottom - safeTop + 1);
		Rectangle protectedArea = new(safeArea.X - 4, safeArea.Y - 4, safeArea.Width + 8, safeArea.Height + 8);
		SpawnSanctuaryReceipt receipt = new(variantId, area, safeArea, centerX, floorY - 3, floorY, 0);
		int cleared = DrainAndBuild(receipt, clearSpawnPocket: false);
		DingDongAuthoredStructureLoader.Generate(Mod, preset, area.Location);
		receipt = receipt with
		{
			LiquidCellsCleared = cleared
		};
		cleared += DrainAndBuild(receipt, clearSpawnPocket: true);
		GenVars.structures.AddProtectedStructure(protectedArea, 0);
		int spawnY = floorY - 3;
		Main.spawnTileX = centerX;
		Main.spawnTileY = spawnY;
		generatedSanctuary = receipt with
		{
			SpawnY = spawnY,
			LiquidCellsCleared = cleared
		};
		Mod.Logger.Info($"DINGDONG_SPAWN_SANCTUARY|verdict=PLACED|variant={variantId}|source=StructureHelper/Structify@{AuthoredPresetCatalog.StructifyCommit}|area={area.X},{area.Y},{area.Width},{area.Height}|spawn={centerX},{spawnY}|floor={floorY}|liquidCellsCleared={cleared}|protected={protectedArea.X},{protectedArea.Y},{protectedArea.Width},{protectedArea.Height}");
	}

	private static void PlaceFoundationTile(int x, int y, bool preserveSolid)
	{
		Tile tile = Main.tile[x, y];
		if (preserveSolid && tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
		{
			tile.LiquidAmount = 0;
			return;
		}

		tile.ClearTile();
		tile.HasTile = true;
		tile.TileType = TileID.StoneSlab;
		tile.LiquidAmount = 0;
		WorldGen.SquareTileFrame(x, y, true);
	}

	private static void PlaceSillTile(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		tile.ClearTile();
		tile.HasTile = true;
		tile.TileType = TileID.GrayBrick;
		tile.LiquidAmount = 0;
		WorldGen.SquareTileFrame(x, y, true);
	}
}
