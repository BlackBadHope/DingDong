using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Runtime-independent reader for the reviewed Structure Helper 3.0.1/3.1.0
/// vanilla subset used by DingDong. It intentionally rejects NBT, custom
/// tile-data blocks and arbitrary modded tile/wall mappings instead of loading
/// another assembly.
/// </summary>
public static partial class DingDongAuthoredStructureLoader
{
	private static Rectangle? FindPrimaryAuthoredFocusBounds(int width, int height, bool[] authoredStructureCell, bool[] authoredHouseWallCell, bool[] authoredRoomNeedCell)
	{
		// Four tiles is required by reviewed multi-room estates whose safe walls
		// are separated by roofs, doors and structural gaps. A global radius of
		// one was contract-tested and rejected because it cropped nine homes.
		const int linkRadius = 4;
		const int captureMargin = 16;
		bool hasHouseWallSeed = Array.IndexOf(authoredHouseWallCell, true) >= 0;
		bool[] focusSeed = hasHouseWallSeed ? authoredHouseWallCell : authoredStructureCell;
		int stride = height + 1;
		int[] prefix = new int[checked((width + 1) * (height + 1))];
		for (int x = 0; x < width; x++)
		{
			int running = 0;
			for (int y = 0; y < height; y++)
			{
				if (focusSeed[x * height + y])
					running++;
				prefix[(x + 1) * stride + (y + 1)] = prefix[x * stride + (y + 1)] + running;
			}
		}

		bool[] envelope = new bool[checked(width * height)];
		for (int x = 0; x < width; x++)
		{
			int left = Math.Max(0, x - linkRadius);
			int right = Math.Min(width - 1, x + linkRadius);
			for (int y = 0; y < height; y++)
			{
				int top = Math.Max(0, y - linkRadius);
				int bottom = Math.Min(height - 1, y + linkRadius);
				int count = prefix[(right + 1) * stride + (bottom + 1)] - prefix[left * stride + (bottom + 1)] - prefix[(right + 1) * stride + top] + prefix[left * stride + top];
				envelope[x * height + y] = count > 0;
			}
		}

		bool[] visited = new bool[checked(width * height)];
		Queue<int> queue = new();
		long bestScore = long.MinValue;
		Rectangle? bestBounds = null;
		for (int startX = 0; startX < width; startX++)
		{
			for (int startY = 0; startY < height; startY++)
			{
				int startIndex = startX * height + startY;
				if (!envelope[startIndex] || visited[startIndex])
					continue;
				visited[startIndex] = true;
				queue.Enqueue(startIndex);
				int artificialCells = 0;
				int houseWallCells = 0;
				int roomNeedCells = 0;
				int minX = width;
				int minY = height;
				int maxX = -1;
				int maxY = -1;
				while (queue.Count > 0)
				{
					int index = queue.Dequeue();
					int x = index / height;
					int y = index % height;
					if (focusSeed[index])
					{
						artificialCells++;
						if (authoredHouseWallCell[index])
							houseWallCells++;
						if (authoredRoomNeedCell[index])
							roomNeedCells++;
						minX = Math.Min(minX, x);
						minY = Math.Min(minY, y);
						maxX = Math.Max(maxX, x);
						maxY = Math.Max(maxY, y);
					}

					EnqueueEnvelope(x - 1, y, width, height, envelope, visited, queue);
					EnqueueEnvelope(x + 1, y, width, height, envelope, visited, queue);
					EnqueueEnvelope(x, y - 1, width, height, envelope, visited, queue);
					EnqueueEnvelope(x, y + 1, width, height, envelope, visited, queue);
				}

				if (artificialCells <= 0)
					continue;
				long score = checked((long)roomNeedCells * 1_000_000L + (long)houseWallCells * 16L + artificialCells);
				if (score <= bestScore)
					continue;
				bestScore = score;
				int left = Math.Max(0, minX - captureMargin);
				int top = Math.Max(0, minY - captureMargin);
				int right = Math.Min(width - 1, maxX + captureMargin);
				int bottom = Math.Min(height - 1, maxY + captureMargin);
				bestBounds = new Rectangle(left, top, right - left + 1, bottom - top + 1);
			}
		}

		return bestBounds;
	}
}
