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
		ClearIntersectedRubble(mod, writePlan, data.Width, data.Height, origin);
		ClearIntersectedDoors(mod, writePlan, data.Width, data.Height, origin);
		bool blend = BlendsWithHost(profile);
		int repainted = 0;
		// Item 6 (28.10): write-time subset of the plan (placement inputs unchanged)
		// and a palette read from the host each cell is written into.
		SceneGroundDrop drop = blend ? BuildSceneGroundDrop(data, writePlan, profile) : null;
		HostBiomeField hostField = UsesHostPalette(profile) ? HostBiomeField.Sample(origin, data.Width, data.Height, profile.TargetBiome) : null;
		HostPalette targetPalette = new(profile?.TargetBiome ?? HomeBiome.Forest, HostEvilKind.None);
		int protectedMasonry = 0, pocketWalls = 0;
		for (int x = 0; x < data.Width; x++)
		{
			for (int y = 0; y < data.Height; y++)
			{
				int index = x * data.Height + y;
				Tile tile = Main.tile[origin.X + x, origin.Y + y];
				TileTypeData tileType = ReadEntry<TileTypeData>(data.TileTypes, index);
				WallTypeData wallType = ReadEntry<WallTypeData>(data.WallTypes, index);
				bool writeTile = writePlan.WriteTiles[index] && (drop == null || !drop.Tiles[index]);
				bool writeWall = writePlan.WriteWalls[index] && (drop == null || !drop.Walls[index]);
				HostPalette palette = targetPalette;
				if (blend && (writeTile || writeWall))
				{
					SourceCell source = ReadSourceCell(data, index);
					bool naturalTile = writeTile && source.HasTile && IsNaturalTerrainTile(source.TileType);
					// The host cell is read before this cell is overwritten.
					if (hostField != null && (naturalTile || (writeWall && source.WallType != WallID.None && IsNaturalBackgroundWall(source.WallType)) || (drop != null && drop.PocketWalls[index])))
						palette = hostField.At(tile, origin.X + x, origin.Y + y);
					if (naturalTile && drop != null && tile.HasTile && IsProtectedHostMasonry(tile.TileType))
					{
						writeTile = false;
						if (writeWall && (source.WallType == WallID.None || IsNaturalBackgroundWall(source.WallType))) writeWall = false;
						protectedMasonry++;
					}
				}
				if (writeTile)
				{
					SetEntry(tile, tileType);
					SetEntry(tile, ReadEntry<LiquidData>(data.Liquids, index));
					SetEntry(tile, ReadEntry<TileWallBrightnessInvisibilityData>(data.BrightnessAndInvisibility, index));
					SetEntry(tile, ReadEntry<TileWallWireStateData>(data.WireState, index));
					if (blend && tile.HasTile && IsNaturalTerrainTile(tile.TileType))
					{
						ushort host = PaletteTile(tile.TileType, palette, profile.TargetBiome);
						if (host != tile.TileType) { tile.TileType = host; repainted++; }
					}
				}

				if (writeWall)
				{
					SetEntry(tile, wallType);
					if (blend && tile.WallType != WallID.None && IsNaturalBackgroundWall(tile.WallType))
					{
						ushort host = PaletteWall(tile.WallType, palette);
						if (host != tile.WallType) { tile.WallType = host; repainted++; }
					}
				}
				// A closed wall-less cave pocket inside retained ground reads as sky.
				if (drop != null && drop.PocketWalls[index] && writeTile && writeWall && !tile.HasTile && tile.WallType == WallID.None)
				{
					tile.WallType = PaletteWall(WallID.DirtUnsafe, palette);
					pocketWalls++;
				}
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
		int backfilled = BackfillsSupport(profile) ? BackfillSurfaceSupport(data, writePlan, origin, drop) : 0;
		int quenched = QuenchesLava(profile) ? QuenchAmbientLava(data, writePlan, origin, profile, drop) : 0;
		if (blend) mod.Logger.Info($"DINGDONG_SCENE_HOST_BLEND|preset={preset.Id}|origin={origin.X},{origin.Y}|target={profile.TargetBiome}|repainted={repainted}|backfilled={backfilled}|quenchedLava={quenched}" +
			$"|palette={(hostField != null ? "host" : "target")}|hostCells={hostField?.HostCells ?? 0}|foreignBaseCells={hostField?.ForeignBaseCells ?? 0}|evilCells={hostField?.EvilCells ?? 0}" +
			$"|droppedGround={drop?.DroppedGround ?? 0}|droppedAmbient={drop?.DroppedAmbient ?? 0}|droppedClearance={drop?.DroppedClearance ?? 0}|droppedFragments={drop?.DroppedFragments ?? 0}|pocketWalls={pocketWalls}|protectedMasonry={protectedMasonry}");
		Rectangle frameBounds = writePlan.MutationBounds;
		frameBounds.Offset(origin);
		RegisterNativeContainer(preset, origin);
		AuthoredSettlementBuilder.FrameAuthoredMutation(frameBounds);
		AuthoredSourceAmbientPolicy.ApplyDefaultOff(mod, preset, origin);
		RequireNativeContainer(preset, origin, requireEmpty: true);
		containers.Commit();
	}
}
