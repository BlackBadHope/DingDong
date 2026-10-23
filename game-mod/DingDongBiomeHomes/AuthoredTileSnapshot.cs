using Terraria;

namespace DingDongBiomeHomes;

// Own the five data planes written by the authored loader, never a Tile handle.
internal readonly record struct AuthoredTileSnapshot(TileTypeData TileType, WallTypeData WallType,
	LiquidData Liquid, TileWallBrightnessInvisibilityData Brightness, TileWallWireStateData State)
{
	internal static AuthoredTileSnapshot Read(Tile tile) => new(tile.Get<TileTypeData>(), tile.Get<WallTypeData>(),
		tile.Get<LiquidData>(), tile.Get<TileWallBrightnessInvisibilityData>(), tile.Get<TileWallWireStateData>());
	internal void Restore(Tile tile)
	{
		tile.Get<TileTypeData>() = TileType; tile.Get<WallTypeData>() = WallType;
		tile.Get<LiquidData>() = Liquid; tile.Get<TileWallBrightnessInvisibilityData>() = Brightness;
		tile.Get<TileWallWireStateData>() = State;
	}
}
