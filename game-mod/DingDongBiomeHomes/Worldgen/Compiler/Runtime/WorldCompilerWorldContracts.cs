using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerWorldContracts
{
	internal static void Prepare(WorldCompilerPlan plan)
	{
		WorldCompilerRoom dungeon = Find(plan, WorldCompilerStructureKind.DungeonRoom);
		WorldCompilerRoom temple = Find(plan, WorldCompilerStructureKind.TempleRoom);
		Main.dungeonX = dungeon.CenterX;
		Main.dungeonY = dungeon.CenterY;
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
			if (!access.Locked)
			{
				Main.dungeonX = access.X + 10;
				Main.dungeonY = access.Top + 5;
			}
		WorldGen.crimson = plan.Resources.Infections[0].Kind ==
			WorldCompilerInfectionKind.Crimson;
		PopulateLegacyLandmarks(plan, dungeon, temple);
		PopulateDungeon(plan, dungeon);
		PopulateSnow(plan);
		// Altar coordinates are published only after the actual multitile is placed.
		ProtectFeatures(plan);
	}

	private static WorldCompilerRoom Find(WorldCompilerPlan plan,
		WorldCompilerStructureKind kind)
	{
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			if (room.Kind == kind) return room;
		throw new InvalidOperationException($"Missing structure contract: {kind}.");
	}

	private static void PopulateDungeon(WorldCompilerPlan plan,
		WorldCompilerRoom first)
	{
		int count = 0;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			if (room.Kind == WorldCompilerStructureKind.DungeonRoom) count++;
		Ensure(ref GenVars.dRoomX, count);
		Ensure(ref GenVars.dRoomY, count);
		Ensure(ref GenVars.dRoomSize, count);
		Ensure(ref GenVars.dRoomL, count);
		Ensure(ref GenVars.dRoomR, count);
		Ensure(ref GenVars.dRoomT, count);
		Ensure(ref GenVars.dRoomB, count);
		int index = 0;
		GenVars.dMinX = first.Bounds.X;
		GenVars.dMaxX = first.Bounds.Right;
		GenVars.dMinY = first.Bounds.Y;
		GenVars.dMaxY = first.Bounds.Bottom;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			if (room.Kind != WorldCompilerStructureKind.DungeonRoom) continue;
			GenVars.dRoomX[index] = room.CenterX;
			GenVars.dRoomY[index] = room.CenterY;
			GenVars.dRoomSize[index] = Math.Max(room.Bounds.Width, room.Bounds.Height);
			GenVars.dRoomL[index] = room.Bounds.X;
			GenVars.dRoomR[index] = room.Bounds.Right;
			GenVars.dRoomT[index] = room.Bounds.Y;
			GenVars.dRoomB[index] = room.Bounds.Bottom;
			GenVars.dMinX = Math.Min(GenVars.dMinX, room.Bounds.X);
			GenVars.dMaxX = Math.Max(GenVars.dMaxX, room.Bounds.Right);
			GenVars.dMinY = Math.Min(GenVars.dMinY, room.Bounds.Y);
			GenVars.dMaxY = Math.Max(GenVars.dMaxY, room.Bounds.Bottom);
			index++;
		}
		GenVars.numDRooms = count;
	}

	private static void PopulateSnow(WorldCompilerPlan plan)
	{
		WorldCompilerBand arctic = default;
		foreach (WorldCompilerBand band in plan.Bands)
			if (band.Biome == WorldCompilerBiome.Arctic) { arctic = band; break; }
		GenVars.snowTop = plan.Surface[(arctic.Left + arctic.Right) / 2];
		GenVars.snowBottom = plan.Height * 4 / 5;
		GenVars.snowOriginLeft = arctic.Left;
		GenVars.snowOriginRight = arctic.Right;
		Ensure(ref GenVars.snowMinX, plan.Height);
		Ensure(ref GenVars.snowMaxX, plan.Height);
		for (int y = 0; y < plan.Height; y++)
		{
			GenVars.snowMinX[y] = arctic.Left;
			GenVars.snowMaxX[y] = arctic.Right;
		}
	}

	private static void ProtectFeatures(WorldCompilerPlan plan)
	{
		foreach (WorldCompilerBasin basin in plan.Hydrology.Basins)
			GenVars.structures.AddProtectedStructure(ToRectangle(basin.Bounds), 4);
		foreach (WorldCompilerDecoration entry in plan.Decorations.Entries)
			if (entry.Kind is WorldCompilerDecorationKind.AmazonTitanTree or
				WorldCompilerDecorationKind.SurfaceChest or
				WorldCompilerDecorationKind.UndergroundChest or
				WorldCompilerDecorationKind.DeepChest)
				GenVars.structures.AddProtectedStructure(ToRectangle(entry.Bounds), 8);
	}

	private static Rectangle ToRectangle(WorldCompilerRect area) =>
		new(area.X, area.Y, area.Width, area.Height);

	private static void Ensure(ref int[] values, int length)
	{
		if (values == null || values.Length < length) Array.Resize(ref values, length);
	}
}
