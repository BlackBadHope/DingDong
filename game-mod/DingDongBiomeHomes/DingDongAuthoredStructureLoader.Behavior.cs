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
	public static AuthoredSceneMaskInfo InspectSceneMask(Mod mod, AuthoredPreset preset)
	{
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		if (data.Width != preset.Width || data.Height != preset.Height)
			throw new InvalidDataException($"Authored preset dimensions changed: {preset.Id} catalog={preset.Width}x{preset.Height}, file={data.Width}x{data.Height}.");
		int total = checked(data.Width * data.Height);
		bool[] content = new bool[total];
		int contentCells = 0;
		for (int index = 0; index < total; index++)
		{
			SourceCell scratch = ReadSourceCell(data, index);
			bool hasContent = scratch.HasTile || scratch.WallType != WallID.None || scratch.LiquidAmount > 0 || scratch.RedWire || scratch.BlueWire || scratch.GreenWire || scratch.YellowWire || scratch.HasActuator;
			content[index] = hasContent;
			if (hasContent)
				contentCells++;
		}

		bool[] exterior = new bool[total];
		Queue<int> queue = new();
		for (int x = 0; x < data.Width; x++)
		{
			EnqueueExterior(x, 0, data.Width, data.Height, content, exterior, queue);
			EnqueueExterior(x, data.Height - 1, data.Width, data.Height, content, exterior, queue);
		}

		for (int y = 1; y < data.Height - 1; y++)
		{
			EnqueueExterior(0, y, data.Width, data.Height, content, exterior, queue);
			EnqueueExterior(data.Width - 1, y, data.Width, data.Height, content, exterior, queue);
		}

		int exteriorCells = 0;
		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			exteriorCells++;
			int x = index / data.Height;
			int y = index % data.Height;
			EnqueueExterior(x - 1, y, data.Width, data.Height, content, exterior, queue);
			EnqueueExterior(x + 1, y, data.Width, data.Height, content, exterior, queue);
			EnqueueExterior(x, y - 1, data.Width, data.Height, content, exterior, queue);
			EnqueueExterior(x, y + 1, data.Width, data.Height, content, exterior, queue);
		}

		int interiorVoidCells = total - contentCells - exteriorCells;
		return new AuthoredSceneMaskInfo(data.Width, data.Height, contentCells, interiorVoidCells, exteriorCells);
	}

	/// <summary>
	/// Returns the exact cells written by <see cref = "Generate"/>. NullBlock and
	/// NullWall cells are excluded, so boundary-connected natural background
	/// preserved by the source export is not treated as a placement collision.
	/// </summary>
	public static AuthoredSceneMutationMask InspectMutationMask(Mod mod, AuthoredPreset preset, AuthoredDeploymentProfile profile = null)
	{
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		if (data.Width != preset.Width || data.Height != preset.Height)
			throw new InvalidDataException($"Authored preset dimensions changed: {preset.Id} catalog={preset.Width}x{preset.Height}, file={data.Width}x{data.Height}.");
		StructureWritePlan writePlan = GetWritePlan(data, preset.ResourcePath, profile);
		return new AuthoredSceneMutationMask(data.Width, data.Height, writePlan.MutationCells, writePlan.MutationCellCount, writePlan.MutationBounds, writePlan.TerrainAnchorY, writePlan.FocusBounds, writePlan.DiscardedMutationCellCount);
	}
}
