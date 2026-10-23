using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerProgressionValidation
{
	internal static void Validate(WorldCompilerPlan plan, WorldCompilerProgressionReceipt receipt)
	{
		if (receipt == null) throw new InvalidOperationException("Progression placement receipt missing.");
		var failures = WorldgenStageSnapshot.Read().Where(item => !item.Healthy).ToArray();
		if (failures.Length > 0)
			throw new InvalidOperationException("Progression integrity failures (objects/chests/obligations/anchors/paths):\n" +
				string.Join("\n", failures.Select(item => item.Key + ": " + item.Detail)));
		WorldCompilerProgressionObjects.ValidateAnchor(receipt, "after-compatibility");
		ValidateObjects(receipt);
		ValidateChests(receipt);
		var paths = new List<WorldCompilerWalkPath>();
		var edges = new List<(int, int)>();
		foreach (WorldCompilerCorridor corridor in plan.Structures.Corridors)
		{
			WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
			WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
			if (from.Kind is not (WorldCompilerStructureKind.DungeonRoom or WorldCompilerStructureKind.TempleRoom)) continue;
			paths.Add(new(from.CenterX, from.CenterY, to.CenterX, to.CenterY));
			edges.Add((from.Id, to.Id));
		}
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
		{
			WorldCompilerRoom room = plan.Structures.Rooms[access.RoomId];
			var nodes = new List<int>();
			foreach (WorldCompilerRoom node in plan.Structures.Rooms)
				if (node.Kind == room.Kind) nodes.Add(node.Id);
			if (!WorldCompilerPathContract.Connected(room.Id, nodes, edges))
				throw new InvalidOperationException($"Disconnected structure graph: {room.Kind}.");
			paths.Add(new(access.X, access.Top, access.TargetX, access.Bottom, true));
		}
		foreach (WorldCompilerWalkPath path in paths)
			if (!WorldCompilerPathContract.TryValidate(path, Passable, out WorldCompilerPoint blocked))
				throw new InvalidOperationException($"Actual structure passage blocked at {blocked.X},{blocked.Y}.");
		receipt.ValidatedPaths = paths.Count;
		receipt.Complete = true;
		WorldCompilerProgressionPersistence.Seal(receipt, paths);
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_WORLD_COMPILER_PROGRESSION|verdict=PASS|objects={receipt.Objects.Count}" +
			$"|chests={receipt.Chests.Count}|paths={paths.Count}|tileRead=True|doorLocked=True" +
			$"|primaryRestorations={receipt.NativePrimaryRestorations}|primaryPolicy=FTW-jungle-AngelStatue-only");
	}

	internal static bool Passable(int x, int y)
	{
		if (x < 1 || y < 1 || x >= Main.maxTilesX - 1 || y >= Main.maxTilesY - 1) return false;
		Tile tile = Main.tile[x, y];
		if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava) return false;
		// The locked gate is checked separately. Navigation models the legitimate unlocked door.
		if (tile.HasTile && tile.TileType == TileID.ClosedDoor) return WorldGen.IsLockedDoor(tile);
		return !tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType];
	}

	internal static void ValidateObjects(WorldCompilerProgressionReceipt receipt)
	{
		WorldCompilerProgressionObligations.Objects(receipt);
		foreach (WorldCompilerProgressionObject entry in receipt.Objects)
		{
			int cells = 0;
			for (int x = entry.Bounds.X; x < entry.Bounds.Right; x++)
			for (int y = entry.Bounds.Y; y < entry.Bounds.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == entry.TileType) cells++;
				if (entry.Name == "temple-locked-door" && !WorldGen.IsLockedDoor(tile))
					throw new InvalidOperationException("Temple entrance is not a locked native door.");
			}
			if (cells != entry.MinimumCells)
				throw new InvalidOperationException($"Progression tile missing: {entry.Name} {cells}/{entry.MinimumCells}.");
		}
	}

	internal static void ValidateChests(WorldCompilerProgressionReceipt receipt)
	{
		WorldCompilerProgressionObligations.Chests(receipt);
		var failures = new List<string>();
		foreach (WorldCompilerProgressionChest entry in receipt.Chests)
		{
			int index = Chest.FindChest(entry.X, entry.Y);
			if (index < 0 || !WorldCompilerChestIntegrity.Present(entry.X, entry.Y) || WorldCompilerChestContents.FindPrimary(Main.chest[index], entry.Primary) < 0 ||
				Chest.IsLocked(entry.X, entry.Y) != entry.Locked)
				failures.Add($"Progression chest missing/changed: {entry.Kind} {entry.X},{entry.Y}:" + WorldCompilerChestReadbackDetail.Describe(entry.X, entry.Y));
		}
		if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
	}
}
