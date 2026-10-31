using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class NativeTerrainPalette
{
	private readonly Color[] tiles = Build(TileLoader.TileCount, MapHelper.tileLookup);
	private readonly Color[] walls = Build(WallLoader.WallCount, MapHelper.wallLookup);
	private static Color[] Build(int count, ushort[] lookup)
	{
		Color[] colors = new Color[count];
		for (int i = 0; i < count; i++)
		{
			if (i < lookup.Length && lookup[i] != 0)
			{
				MapTile entry = MapTile.Create(lookup[i], 255, 0);
				colors[i] = MapHelper.GetMapTileXnaColor(ref entry);
			}
			else
			{
				uint hash = unchecked((uint)i * 2654435761u);
				colors[i] = new Color(60 + (int)(hash & 127), 60 + (int)((hash >> 8) & 127), 60 + (int)((hash >> 16) & 127));
			}
		}
		return colors;
	}
	internal Color At(int x, int y)
	{
		return At(Main.tile[x, y], y);
	}
	internal Color At(Tile tile, int y)
	{
		return At(tile.HasTile, tile.TileType, tile.WallType, tile.LiquidAmount, tile.LiquidType, y);
	}
	internal Color At(bool active, ushort type, ushort wall, byte liquid, int liquidType, int y)
	{
		if (active) return type < tiles.Length ? tiles[type] : Color.Magenta;
		if (liquid > 0)
		{
			Color fluid = liquidType switch {
				1 => new Color(245, 75, 20), 2 => new Color(195, 135, 25),
				3 => new Color(165, 115, 220), _ => new Color(25, 75, 180) };
			// S34: a background wall behind the liquid shows through, as it does in game.
			return wall != Terraria.ID.WallID.None && wall < walls.Length ? Color.Lerp(fluid, walls[wall], 0.35f) : fluid;
		}
		if (wall != Terraria.ID.WallID.None) return wall < walls.Length ? walls[wall] * 0.45f : Color.Magenta;
		return y < Main.worldSurface ? new Color(100, 150, 235) : new Color(18, 23, 27);
	}
}
