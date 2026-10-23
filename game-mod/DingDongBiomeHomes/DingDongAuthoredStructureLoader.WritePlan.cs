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
	private static StructureWritePlan BuildWritePlan(StructureData data, bool integrateNaturalBoundary)
	{
		int total = checked(data.Width * data.Height);
		bool[] rawWritesTile = new bool[total];
		bool[] rawWritesWall = new bool[total];
		bool[] terrainTileLayer = new bool[total];
		bool[] terrainWallLayer = new bool[total];
		bool[] sourceNaturalTile = new bool[total];
		bool[] sourceNaturalWall = new bool[total];
		bool[] exteriorCompatible = new bool[total];
		bool[] naturalSolid = new bool[total];
		bool[] authoredStructureCell = new bool[total];
		bool[] authoredHouseWallCell = new bool[total];
		bool[] authoredRoomNeedCell = new bool[total];
		HashSet<int> roomNeedTileTypes = new(TileID.Sets.RoomNeeds.CountsAsChair);
		roomNeedTileTypes.UnionWith(TileID.Sets.RoomNeeds.CountsAsTable);
		roomNeedTileTypes.UnionWith(TileID.Sets.RoomNeeds.CountsAsTorch);
		bool[] writeTiles, writeWalls, mutationCells;
		ClassifySourceCellsAndResolveWriteLayers(data, integrateNaturalBoundary, total, rawWritesTile, rawWritesWall, terrainTileLayer, terrainWallLayer, sourceNaturalTile, sourceNaturalWall, exteriorCompatible, naturalSolid, authoredStructureCell, authoredHouseWallCell, authoredRoomNeedCell, roomNeedTileTypes, out writeTiles, out writeWalls, out mutationCells);
		// The former largest-house-wall rectangle cut through roofs, towers and
		// other rooms. Spatial planning must admit the complete written scene.
		Rectangle? focusBounds = null;
		int discardedMutationCellCount = 0;

		int mutationCellCount = 0;
		int minX = data.Width;
		int minY = data.Height;
		int maxX = -1;
		int maxY = -1;
		for (int x = 0; x < data.Width; x++)
		{
			for (int y = 0; y < data.Height; y++)
			{
				int index = x * data.Height + y;
				bool mutates = mutationCells[index];
				if (!mutates)
					continue;
				mutationCellCount++;
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		if (mutationCellCount <= 0)
			throw new InvalidDataException("Authored structure integration removed every mutation cell.");
		Rectangle mutationBounds = new(minX, minY, maxX - minX + 1, maxY - minY + 1);
		if (integrateNaturalBoundary) focusBounds = mutationBounds;
		int? terrainAnchorY = integrateNaturalBoundary ? FindTerrainAnchorY(data.Width, data.Height, naturalSolid, focusBounds) : null;
		return new StructureWritePlan(writeTiles, writeWalls, mutationCells, mutationCellCount, mutationBounds, terrainAnchorY, focusBounds, discardedMutationCellCount);
	}
}
