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
	private static void EnqueueEnvelope(int x, int y, int width, int height, bool[] envelope, bool[] visited, Queue<int> queue)
	{
		if (x < 0 || y < 0 || x >= width || y >= height)
			return;
		int index = x * height + y;
		if (!envelope[index] || visited[index])
			return;
		visited[index] = true;
		queue.Enqueue(index);
	}

	private static void EnqueueCompatible(int x, int y, int width, int height, bool[] compatible, bool[] connected, Queue<int> queue)
	{
		if (x < 0 || y < 0 || x >= width || y >= height)
			return;
		int index = x * height + y;
		if (!compatible[index] || connected[index])
			return;
		connected[index] = true;
		queue.Enqueue(index);
	}

	private static void EnqueueExterior(int x, int y, int width, int height, bool[] content, bool[] exterior, Queue<int> queue)
	{
		if (x < 0 || y < 0 || x >= width || y >= height)
			return;
		int index = x * height + y;
		if (content[index] || exterior[index])
			return;
		exterior[index] = true;
		queue.Enqueue(index);
	}
}
