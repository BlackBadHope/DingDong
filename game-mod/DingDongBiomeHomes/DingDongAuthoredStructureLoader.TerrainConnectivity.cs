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
	private static bool[] FindBoundaryConnectedCells(int width, int height, bool[] compatible)
	{
		bool[] connected = new bool[checked(width * height)];
		Queue<int> queue = new();
		for (int x = 0; x < width; x++)
		{
			EnqueueCompatible(x, 0, width, height, compatible, connected, queue);
			EnqueueCompatible(x, height - 1, width, height, compatible, connected, queue);
		}

		for (int y = 1; y < height - 1; y++)
		{
			EnqueueCompatible(0, y, width, height, compatible, connected, queue);
			EnqueueCompatible(width - 1, y, width, height, compatible, connected, queue);
		}

		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			int x = index / height;
			int y = index % height;
			EnqueueCompatible(x - 1, y, width, height, compatible, connected, queue);
			EnqueueCompatible(x + 1, y, width, height, compatible, connected, queue);
			EnqueueCompatible(x, y - 1, width, height, compatible, connected, queue);
			EnqueueCompatible(x, y + 1, width, height, compatible, connected, queue);
		}

		return connected;
	}

	private static int? FindTerrainAnchorY(int width, int height, bool[] naturalSolid, Rectangle? focusBounds)
	{
		List<int> seamRows = new();
		Rectangle sampleBounds = focusBounds ?? new Rectangle(0, 0, width, height);
		int centralLeft = sampleBounds.Left + sampleBounds.Width / 5;
		int centralRight = sampleBounds.Right - sampleBounds.Width / 5;
		int startY = sampleBounds.Top + sampleBounds.Height / 2;
		for (int x = centralLeft; x < centralRight; x++)
		{
			int sampleLeft = Math.Max(0, x - 4);
			int sampleRight = Math.Min(width - 1, x + 4);
			for (int y = startY; y <= Math.Min(height - 12, sampleBounds.Bottom - 12); y++)
			{
				int occupied = 0;
				int samples = 0;
				for (int sampleX = sampleLeft; sampleX <= sampleRight; sampleX++)
				{
					for (int sampleY = y; sampleY < y + 12; sampleY++)
					{
						samples++;
						if (naturalSolid[sampleX * height + sampleY])
							occupied++;
					}
				}

				if (occupied < Math.Ceiling(samples * 0.55))
					continue;
				seamRows.Add(y);
				break;
			}
		}

		if (seamRows.Count == 0)
			return null;
		seamRows.Sort();
		return seamRows[seamRows.Count / 2];
	}
}
