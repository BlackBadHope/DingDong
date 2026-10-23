using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerWorldContracts
{
	private static void PopulateLegacyLandmarks(WorldCompilerPlan plan,
		WorldCompilerRoom dungeon, WorldCompilerRoom temple)
	{
		PopulateSurfaceLayers(plan);
		PopulateBiomeBounds(plan);
		PopulatePyramids(plan);
		GenVars.dungeonX = Main.dungeonX;
		GenVars.dungeonY = Main.dungeonY;
		GenVars.dungeonLocation = Main.dungeonX;
		GenVars.dungeonSide = dungeon.CenterX < plan.Width / 2 ? -1 : 1;
		GenVars.tLeft = temple.Bounds.X;
		GenVars.tRight = temple.Bounds.Right;
		GenVars.tTop = temple.Bounds.Y;
		GenVars.tBottom = temple.Bounds.Bottom;
		GenVars.tRooms = Count(plan, WorldCompilerStructureKind.TempleRoom);
	}

	private static void PopulateSurfaceLayers(WorldCompilerPlan plan)
	{
		int low = plan.Surface[0];
		int high = low;
		for (int x = 0; x < plan.Surface.Width; x++)
		{
			int value = plan.Surface[x];
			low = Math.Min(low, value);
			high = Math.Max(high, value);
		}
		GenVars.worldSurfaceLow = low;
		GenVars.worldSurface = Main.worldSurface;
		GenVars.worldSurfaceHigh = high;
		GenVars.rockLayerLow = Main.rockLayer - plan.Height / 30;
		GenVars.rockLayer = Main.rockLayer;
		GenVars.rockLayerHigh = Main.rockLayer + plan.Height / 30;
		GenVars.waterLine = (int)Main.worldSurface + plan.Height / 5;
		GenVars.lavaLine = plan.Height * 4 / 5;
	}

	private static void PopulateBiomeBounds(WorldCompilerPlan plan)
	{
		WorldCompilerBand amazon = Band(plan, WorldCompilerBiome.Amazon);
		WorldCompilerBand sahara = Band(plan, WorldCompilerBiome.Sahara);
		GenVars.jungleOriginX = (amazon.Left + amazon.Right) / 2;
		GenVars.jungleMinX = amazon.Left;
		GenVars.jungleMaxX = amazon.Right;
		GenVars.JungleX = GenVars.jungleOriginX;
		GenVars.leftBeachEnd = plan.Bands[0].Right;
		GenVars.rightBeachStart = plan.Bands[^1].Left;
		int desertTop = plan.Surface[(sahara.Left + sahara.Right) / 2];
		int desertHeight = Math.Max(240, plan.Height / 7);
		GenVars.UndergroundDesertLocation = new Rectangle(sahara.Left,
			desertTop, sahara.Width, desertHeight);
		GenVars.UndergroundDesertHiveLocation = GenVars.UndergroundDesertLocation;
		GenVars.desertHiveLeft = sahara.Left;
		GenVars.desertHiveRight = sahara.Right;
		GenVars.desertHiveHigh = desertTop;
		GenVars.desertHiveLow = desertTop + desertHeight;
	}

	private static void PopulatePyramids(WorldCompilerPlan plan)
	{
		int count = Count(plan, WorldCompilerStructureKind.DesertPyramid);
		Ensure(ref GenVars.PyrX, Math.Max(32, count + 8));
		Ensure(ref GenVars.PyrY, Math.Max(32, count + 8));
		int index = 0;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			if (room.Kind != WorldCompilerStructureKind.DesertPyramid) continue;
			GenVars.PyrX[index] = room.CenterX;
			GenVars.PyrY[index] = room.Bounds.Y;
			index++;
		}
		GenVars.numPyr = count;
	}

	private static WorldCompilerBand Band(WorldCompilerPlan plan,
		WorldCompilerBiome biome)
	{
		foreach (WorldCompilerBand band in plan.Bands)
			if (band.Biome == biome) return band;
		throw new InvalidOperationException($"Missing biome contract: {biome}.");
	}

	private static int Count(WorldCompilerPlan plan, WorldCompilerStructureKind kind)
	{
		int count = 0;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
			if (room.Kind == kind) count++;
		return count;
	}
}
