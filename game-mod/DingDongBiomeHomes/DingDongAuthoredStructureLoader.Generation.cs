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
	public static void Generate(Mod mod, AuthoredPreset preset, Point origin, AuthoredDeploymentProfile profile = null)
	{
		if (preset.ContainsNbt)
			throw new InvalidOperationException($"Authored preset '{preset.Id}' contains NBT and is withheld until DingDong's explicit TileEntity importer is implemented.");
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		StructureWritePlan writePlan = GetWritePlan(data, preset.ResourcePath, profile);
		if (data.Width != preset.Width || data.Height != preset.Height)
			throw new InvalidDataException($"Authored preset dimensions changed: {preset.Id} catalog={preset.Width}x{preset.Height}, file={data.Width}x{data.Height}.");
		if (origin.X < 0 || origin.Y < 0 || origin.X + data.Width >= Main.maxTilesX || origin.Y + data.Height >= Main.maxTilesY)
			throw new ArgumentOutOfRangeException(nameof(origin), $"Authored preset '{preset.Id}' would be placed outside the world at {origin.X},{origin.Y}.");
		PreflightNativeContainer(preset, origin);
		using var containers = AuthoredNativeContainerTransaction.Begin();
		ClearIntersectedFallenLogs(mod, writePlan, data.Width, data.Height, origin);
		for (int x = 0; x < data.Width; x++)
		{
			for (int y = 0; y < data.Height; y++)
			{
				int index = x * data.Height + y;
				Tile tile = Main.tile[origin.X + x, origin.Y + y];
				TileTypeData tileType = ReadEntry<TileTypeData>(data.TileTypes, index);
				WallTypeData wallType = ReadEntry<WallTypeData>(data.WallTypes, index);
				if (writePlan.WriteTiles[index])
				{
					SetEntry(tile, tileType);
					SetEntry(tile, ReadEntry<LiquidData>(data.Liquids, index));
					SetEntry(tile, ReadEntry<TileWallBrightnessInvisibilityData>(data.BrightnessAndInvisibility, index));
					SetEntry(tile, ReadEntry<TileWallWireStateData>(data.WireState, index));
				}

				if (writePlan.WriteWalls[index])
					SetEntry(tile, wallType);
				if (tile.HasTile && tile.TileType >= TileLoader.TileCount)
				{
					throw new InvalidDataException($"Authored preset '{preset.Id}' has active tile ID {tile.TileType} outside the loaded tile range at {x},{y}.");
				}

				if (tile.WallType >= WallLoader.WallCount)
				{
					throw new InvalidDataException($"Authored preset '{preset.Id}' has wall ID {tile.WallType} outside the loaded wall range at {x},{y}.");
				}
			}
		}
		Rectangle frameBounds = writePlan.MutationBounds;
		frameBounds.Offset(origin);
		RegisterNativeContainer(preset, origin);
		AuthoredSettlementBuilder.FrameAuthoredMutation(frameBounds);
		AuthoredSourceAmbientPolicy.ApplyDefaultOff(mod, preset, origin);
		RequireNativeContainer(preset, origin, requireEmpty: true);
		containers.Commit();
	}
}
