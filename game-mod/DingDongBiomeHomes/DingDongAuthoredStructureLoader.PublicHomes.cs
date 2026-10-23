using System;
using System.IO;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private static StructureData CreatePublicHome(string resourcePath)
	{
		if (!PublicHomeReplacementCatalog.TryResolve(resourcePath, out AuthoredPreset preset))
			throw new InvalidDataException($"Unknown public procedural home: {resourcePath}.");
		PublicHomeReplacementPlan plan = new(preset.Id, preset.Width, preset.Height, preset.PlacementMode);
		int count = checked(plan.Width * plan.Height);
		TileTypeData[] tiles = new TileTypeData[count];
		WallTypeData[] walls = new WallTypeData[count];
		TileWallWireStateData[] state = new TileWallWireStateData[count];
		// This sentinel never enters Main.tile: Preserve cells are excluded from write masks.
		const ushort preserve = ushort.MaxValue;
		ushort material = plan.Palette switch
		{
			0 => TileID.WoodBlock,
			1 => TileID.GrayBrick,
			2 => TileID.RedBrick,
			_ => TileID.StoneSlab
		};
		for (int index = 0; index < count; index++)
		{
			PublicHomeCell cell = plan.Cells[index];
			if (cell.Tile == PublicHomeTile.Preserve)
			{
				tiles[index].Type = preserve; walls[index].Type = preserve;
				continue;
			}
			tiles[index].Type = cell.Tile switch
			{
				PublicHomeTile.Air => 0,
				PublicHomeTile.Block => material,
				PublicHomeTile.Platform => TileID.Platforms,
				PublicHomeTile.ClosedDoor => TileID.ClosedDoor,
				PublicHomeTile.Chair => TileID.Chairs,
				PublicHomeTile.Table => TileID.Tables,
				PublicHomeTile.Torch => TileID.Torches,
				PublicHomeTile.Glass => TileID.Glass,
				_ => throw new InvalidDataException("Unknown public home cell token.")
			};
			walls[index].Type = cell.SafeWall ? WallID.Wood : (ushort)0;
			state[index].HasTile = cell.Tile != PublicHomeTile.Air;
			state[index].TileFrameX = cell.FrameX;
			state[index].TileFrameY = cell.FrameY;
		}
		return new StructureData(plan.Width, plan.Height, preserve, preserve,
			MemoryMarshal.AsBytes(tiles.AsSpan()).ToArray(), MemoryMarshal.AsBytes(walls.AsSpan()).ToArray(),
			MemoryMarshal.AsBytes(new LiquidData[count].AsSpan()).ToArray(),
			MemoryMarshal.AsBytes(new TileWallBrightnessInvisibilityData[count].AsSpan()).ToArray(),
			MemoryMarshal.AsBytes(state.AsSpan()).ToArray());
	}
}
