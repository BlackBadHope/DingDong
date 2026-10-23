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
	private static StructureData Read(BinaryReader reader, string resourcePath)
	{
		string header = reader.ReadString();
		if (!string.Equals(header, Header, StringComparison.Ordinal))
			throw new InvalidDataException($"Unsupported authored structure header in '{resourcePath}'.");
		int width = reader.ReadInt32();
		int height = reader.ReadInt32();
		bool containsNbt = reader.ReadBoolean();
		string version = reader.ReadString();
		if (!SupportedVersions.Contains(version))
			throw new InvalidDataException($"Unsupported authored structure version in '{resourcePath}': {version}.");
		if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
			throw new InvalidDataException($"Unsafe authored structure dimensions in '{resourcePath}': {width}x{height}.");
		ushort? nullTileId = ReadMappingTable(reader, NullTileKey, "tile", resourcePath);
		ushort? nullWallId = ReadMappingTable(reader, NullWallKey, "wall", resourcePath);
		for (int x = 0; x < width; x++)
			reader.ReadBoolean();
		int entryCount = reader.ReadInt32();
		if (entryCount != 5)
			throw new InvalidDataException($"Authored structure '{resourcePath}' has {entryCount} tile-data blocks; exactly 5 vanilla blocks are required.");
		Dictionary<string, byte[]> blocks = new(StringComparer.Ordinal);
		for (int index = 0; index < entryCount; index++)
		{
			string name = reader.ReadString();
			int length = reader.ReadInt32();
			if (length < 0 || length > 256 * 1024 * 1024)
				throw new InvalidDataException($"Unsafe block length in '{resourcePath}': {name}={length}.");
			if (!blocks.TryAdd(name, reader.ReadBytes(length)))
				throw new InvalidDataException($"Duplicate tile-data block in '{resourcePath}': {name}.");
			if (blocks[name].Length != length)
				throw new EndOfStreamException($"Truncated tile-data block in '{resourcePath}': {name}.");
		}

		if (containsNbt)
			throw new InvalidDataException($"Authored structure '{resourcePath}' contains NBT; explicit DingDong import is required.");
		int cellCount = checked(width * height);
		return new StructureData(width, height, nullTileId, nullWallId, RequireBlock<TileTypeData>(blocks, "Terraria/TileTypeData", cellCount, resourcePath), RequireBlock<WallTypeData>(blocks, "Terraria/WallTypeData", cellCount, resourcePath), RequireBlock<LiquidData>(blocks, "Terraria/LiquidData", cellCount, resourcePath), RequireBlock<TileWallBrightnessInvisibilityData>(blocks, "Terraria/TileWallBrightnessInvisibilityData", cellCount, resourcePath), RequireBlock<TileWallWireStateData>(blocks, "Terraria/TileWallWireStateData", cellCount, resourcePath));
	}

	private static ushort? ReadMappingTable(BinaryReader reader, string permittedKey, string kind, string resourcePath)
	{
		int count = reader.ReadInt32();
		ushort? nullId = null;
		for (int index = 0; index < count; index++)
		{
			ushort localId = reader.ReadUInt16();
			string key = reader.ReadString();
			if (!string.Equals(key, permittedKey, StringComparison.Ordinal))
				throw new InvalidDataException($"Authored structure '{resourcePath}' references unsupported modded {kind} '{key}'.");
			if (nullId.HasValue)
				throw new InvalidDataException($"Authored structure '{resourcePath}' has duplicate null {kind} mappings.");
			nullId = localId;
		}

		return nullId;
	}

	private static T ReadEntry<T>(byte[] data, int index)
		where T : unmanaged, ITileData
	{
		int size = Unsafe.SizeOf<T>();
		return MemoryMarshal.Read<T>(data.AsSpan(checked(index * size), size));
	}
}
