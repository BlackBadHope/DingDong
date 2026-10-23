using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsGemPlacement
{
	private const int TempleTileLimit = 2_000_000;

	internal static void PlaceTopaz()
	{
		var plan = WorldCompilerRuntimeState.Current;
		if (plan != null)
		{
			(int x, int y) = SotsTempleRoof.Find(plan.Structures.Rooms,
				GenVars.lAltarX, GenVars.lAltarY, IsTemple);
			GenerateTopaz(x, y);
			SotsWorldgenReceipt.MarkTopaz();
			DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_SOTS_TOPAZ|verdict=PLACED|anchor={x},{y}|source=compiler-roof|tileRead=True");
			return;
		}
		(int roofX, int roofY) = FindNativeRoof();
		GenerateTopaz(roofX, roofY);
		SotsWorldgenReceipt.MarkTopaz();
	}

	internal static (int X, int Y) FindNativeRoof()
	{
		(int seedX, int seedY) = NativeTempleLocator.FindBrick();
		HashSet<int> temple = FloodTemple(seedX, seedY, out int minY, out int minX, out int maxX);
		for (int y = minY; y <= Math.Min(minY + 64, Main.maxTilesY - 100); y++)
		{
			(int start, int length) = LongestRun(temple, y, minX, maxX);
			if (length < 4)
				continue;
			return (start + length / 2, y);
		}
		throw new InvalidOperationException("SOTS Topaz anchor has no four-tile temple roof.");
	}

	private static HashSet<int> FloodTemple(int seedX, int seedY,
		out int minY, out int minX, out int maxX)
	{
		int width = Main.maxTilesX;
		Queue<int> queue = new();
		HashSet<int> visited = [];
		int seed = seedY * width + seedX;
		queue.Enqueue(seed);
		visited.Add(seed);
		minY = seedY;
		minX = maxX = seedX;
		while (queue.Count != 0)
		{
			int key = queue.Dequeue();
			int x = key % width;
			int y = key / width;
			minY = Math.Min(minY, y);
			minX = Math.Min(minX, x);
			maxX = Math.Max(maxX, x);
			AddTempleNeighbor(x - 1, y, width, visited, queue);
			AddTempleNeighbor(x + 1, y, width, visited, queue);
			AddTempleNeighbor(x, y - 1, width, visited, queue);
			AddTempleNeighbor(x, y + 1, width, visited, queue);
			if (visited.Count > TempleTileLimit)
				throw new InvalidOperationException("SOTS Topaz temple component exceeded its bounded tile budget.");
		}
		return visited;
	}

	private static void AddTempleNeighbor(int x, int y, int width,
		HashSet<int> visited, Queue<int> queue)
	{
		if (!WorldGen.InWorld(x, y, 100) || !IsTemple(x, y)) return;
		int key = y * width + x;
		if (visited.Add(key)) queue.Enqueue(key);
	}

	private static bool IsTemple(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && tile.TileType == TileID.LihzahrdBrick;
	}

	private static (int Start, int Length) LongestRun(HashSet<int> tiles,
		int y, int left, int right)
	{
		int width = Main.maxTilesX;
		int bestStart = -1, bestLength = 0, start = -1;
		for (int x = left; x <= right + 1; x++)
		{
			if (x <= right && tiles.Contains(y * width + x))
			{
				if (start < 0) start = x;
				continue;
			}
			if (start >= 0 && x - start > bestLength)
				(bestStart, bestLength) = (start, x - start);
			start = -1;
		}
		return (bestStart, bestLength);
	}
}
