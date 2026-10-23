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
	private static void ClassifySourceCell(StructureData data, bool[] rawWritesTile, bool[] rawWritesWall, bool[] terrainTileLayer, bool[] terrainWallLayer, bool[] sourceNaturalTile, bool[] sourceNaturalWall, bool[] exteriorCompatible, bool[] naturalSolid, bool[] authoredStructureCell, bool[] authoredHouseWallCell, bool[] authoredRoomNeedCell, HashSet<int> roomNeedTileTypes, SourceCell scratch, int index, TileTypeData tileType, WallTypeData wallType, bool writesTile)
	{
		bool writesWall = !data.NullWallId.HasValue || wallType.Type != data.NullWallId.Value;
		rawWritesTile[index] = writesTile;
		rawWritesWall[index] = writesWall;
		bool naturalTile = scratch.HasTile && IsNaturalTerrainTile(scratch.TileType);
		bool naturalWall = scratch.WallType == WallID.None || IsNaturalBackgroundWall(scratch.WallType);
		bool tileLayerIsTerrain = !writesTile || !scratch.HasTile || naturalTile;
		bool wallLayerIsTerrain = !writesWall || naturalWall;
		bool hasAuthoredMechanism = scratch.RedWire || scratch.BlueWire || scratch.GreenWire || scratch.YellowWire || scratch.HasActuator;
		terrainTileLayer[index] = tileLayerIsTerrain;
		terrainWallLayer[index] = wallLayerIsTerrain;
		sourceNaturalTile[index] = writesTile && scratch.HasTile && naturalTile;
		sourceNaturalWall[index] = writesWall && naturalWall;
		exteriorCompatible[index] = tileLayerIsTerrain && wallLayerIsTerrain && !hasAuthoredMechanism;
		naturalSolid[index] = naturalTile;
		authoredStructureCell[index] = hasAuthoredMechanism || (writesTile && scratch.HasTile && !naturalTile) || (writesWall && scratch.WallType != WallID.None && !naturalWall);
		authoredHouseWallCell[index] = writesWall && scratch.WallType != WallID.None && scratch.WallType < Main.wallHouse.Length && Main.wallHouse[scratch.WallType];
		authoredRoomNeedCell[index] = writesTile && scratch.HasTile && roomNeedTileTypes.Contains(scratch.TileType);
	}

}
