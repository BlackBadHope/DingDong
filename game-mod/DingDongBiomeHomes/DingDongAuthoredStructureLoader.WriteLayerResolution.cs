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
	private static void ClassifySourceCellsAndResolveWriteLayers(StructureData data, bool integrateNaturalBoundary, int total, bool[] rawWritesTile, bool[] rawWritesWall, bool[] terrainTileLayer, bool[] terrainWallLayer, bool[] sourceNaturalTile, bool[] sourceNaturalWall, bool[] exteriorCompatible, bool[] naturalSolid, bool[] authoredStructureCell, bool[] authoredHouseWallCell, bool[] authoredRoomNeedCell, HashSet<int> roomNeedTileTypes, out bool[] writeTiles, out bool[] writeWalls, out bool[] mutationCells)
	{
		roomNeedTileTypes.UnionWith(TileID.Sets.RoomNeeds.CountsAsDoor);
		for (int index = 0; index < total; index++)
		{
			SourceCell scratch = ReadSourceCell(data, index);
			TileTypeData tileType = ReadEntry<TileTypeData>(data.TileTypes, index);
			WallTypeData wallType = ReadEntry<WallTypeData>(data.WallTypes, index);
			bool writesTile = !data.NullTileId.HasValue || tileType.Type != data.NullTileId.Value;
			ClassifySourceCell(data, rawWritesTile, rawWritesWall, terrainTileLayer, terrainWallLayer, sourceNaturalTile, sourceNaturalWall, exteriorCompatible, naturalSolid, authoredStructureCell, authoredHouseWallCell, authoredRoomNeedCell, roomNeedTileTypes, scratch, index, tileType, wallType, writesTile);
		}

		bool[] boundaryTerrain = integrateNaturalBoundary ? FindBoundaryConnectedCells(data.Width, data.Height, exteriorCompatible) : new bool[total];
		int[] distance = integrateNaturalBoundary ? AuthoredSceneEnvelope.Distances(data.Width, data.Height, authoredStructureCell) : null;
		writeTiles = new bool[total];
		writeWalls = new bool[total];
		mutationCells = new bool[total];
		for (int index = 0; index < total; index++)
		{
			bool nearSupport = integrateNaturalBoundary && distance[index] <= AuthoredSceneEnvelope.SupportRadius;
			bool nearClearance = integrateNaturalBoundary && distance[index] <= AuthoredSceneEnvelope.ClearanceRadius;
			// Natural material may be a house foundation, cliff, living-tree root,
			// or furniture anchor. Stripping it globally leaves floating fragments.
			bool retainTile = !integrateNaturalBoundary || !terrainTileLayer[index] || !boundaryTerrain[index] ||
				(sourceNaturalTile[index] ? nearSupport : nearClearance);
			bool retainWall = !integrateNaturalBoundary || !terrainWallLayer[index] || !boundaryTerrain[index] || nearClearance;
			writeTiles[index] = rawWritesTile[index] && retainTile;
			writeWalls[index] = rawWritesWall[index] && retainWall;
			mutationCells[index] = writeTiles[index] || writeWalls[index];
		}
	}
}
