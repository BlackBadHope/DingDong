using System;
using System.Linq;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem
{
	private void CompleteReceipt(GenerationProgress progress, GameConfiguration _)
	{
		progress.Message = "Sealing DingDong world compiler receipt";
		WorldCompilerProgressionValidation.Validate(Plan, Receipt.Progression);
		if (Receipt.Compatibility == null || !Receipt.Compatibility.Complete)
			throw new InvalidOperationException(
				$"Compatibility passes are incomplete: " +
				$"{Receipt.Compatibility?.Completed ?? 0}/" +
				$"{Receipt.Compatibility?.Expected ?? Receipt.CompatibilityPasses}.");
		Receipt.Equivalents = CountCompilerEquivalents();
		if (!Receipt.Equivalents.Complete)
			throw new InvalidOperationException(
				$"Compiler equivalents are incomplete: {Receipt.Equivalents.Value}.");
		int expectedChests = Plan.Decorations.Entries.Count(entry => entry.Kind is
			WorldCompilerDecorationKind.SurfaceChest or
			WorldCompilerDecorationKind.UndergroundChest or
			WorldCompilerDecorationKind.DeepChest);
		var snapshot = new WorldCompilerReceiptSnapshot(
			Receipt.Terrain.SolidTiles, Receipt.Caves.CarvedTiles,
			Receipt.Resources.OreTiles, Receipt.Hydrology.LiquidTiles,
			Receipt.Structures.Rooms, Plan.Structures.Rooms.Length,
			Receipt.Decorations.Chests, expectedChests, Receipt.Finalized);
		if (!WorldCompilerReceiptContract.TryValidate(snapshot, out string failure))
			throw new InvalidOperationException(
				$"World compiler receipt is incomplete: {failure}.");
		Receipt.Complete = true;
		Mod.Logger.Info($"DINGDONG_WORLD_COMPILER|verdict=PASS" +
			$"|fingerprint={Plan.Fingerprint:X16}|world={Plan.Width}x{Plan.Height}" +
			$"|chunks={Plan.Chunks.Length}|caves={Plan.Caves.Nodes.Length}" +
			$"|rooms={Plan.Structures.Rooms.Length}|basins={Plan.Hydrology.Basins.Length}" +
			$"|deposits={Plan.Resources.Deposits.Length}" +
			$"|decorations={Plan.Decorations.Entries.Length}" +
			$"|compatibility={Receipt.CompatibilityPasses}" +
			$"|compatibilityCompleted={Receipt.Compatibility.Completed}" +
			$"|compatibilityFingerprint={Receipt.Compatibility.Fingerprint:X16}" +
			$"|equivalents={Receipt.Equivalents.Value}" +
			$"|extensions={Receipt.ExtensionActions}|vanillaPasses=0");
		WorldCompilerRuntimeState.PublishAndReleasePlan();
		Mod.Logger.Info("DINGDONG_WORLD_COMPILER_MEMORY|verdict=PASS" +
			"|fullPlanReleased=True|apiViewRetained=True");
	}

	private static WorldCompilerEquivalentReceipt CountCompilerEquivalents()
	{
		int spawn = 0, dungeon = 0, temple = 0, desert = 0, mushroom = 0;
		foreach (WorldCompilerRoom room in Plan.Structures.Rooms)
			switch (room.Kind)
			{
				case WorldCompilerStructureKind.SpawnSanctuary: spawn++; break;
				case WorldCompilerStructureKind.DungeonRoom: dungeon++; break;
				case WorldCompilerStructureKind.TempleRoom: temple++; break;
				case WorldCompilerStructureKind.DesertPyramid: desert++; break;
			}
		foreach (WorldCompilerDecoration entry in Plan.Decorations.Entries)
			if (entry.Kind == WorldCompilerDecorationKind.MushroomCavern) mushroom++;
		return new WorldCompilerEquivalentReceipt(spawn, dungeon, temple, desert,
			mushroom);
	}
}
