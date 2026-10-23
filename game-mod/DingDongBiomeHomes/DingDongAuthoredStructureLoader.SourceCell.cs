using Terraria;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	// Terraria.Tile is an indexed world handle, NOT an independent value buffer.
	// new Tile() aliases world tile 0. Decode into owned values without touching Main.tile.
	private readonly record struct SourceCell(ushort TileType, ushort WallType,
		TileWallWireStateData State, LiquidData Liquid)
	{
		internal bool HasTile => State.HasTile;
		internal byte LiquidAmount => Liquid.Amount;
		internal int LiquidType => Liquid.LiquidType;
		internal bool RedWire => State.RedWire;
		internal bool BlueWire => State.BlueWire;
		internal bool GreenWire => State.GreenWire;
		internal bool YellowWire => State.YellowWire;
		internal bool HasActuator => State.HasActuator;
	}
	private static SourceCell ReadSourceCell(StructureData data, int index)
	{
		ushort tile = ReadEntry<TileTypeData>(data.TileTypes, index).Type;
		ushort wall = ReadEntry<WallTypeData>(data.WallTypes, index).Type;
		bool writesTile = !data.NullTileId.HasValue || tile != data.NullTileId.Value;
		bool writesWall = !data.NullWallId.HasValue || wall != data.NullWallId.Value;
		return new SourceCell(writesTile ? tile : (ushort)0, writesWall ? wall : (ushort)0,
			writesTile ? ReadEntry<TileWallWireStateData>(data.WireState, index) : default,
			writesTile ? ReadEntry<LiquidData>(data.Liquids, index) : default);
	}
}
