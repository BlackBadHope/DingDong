using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static Point16 ResolveVillageEntrance(Point16 original, bool found, object corruption)
	{
		if (!ContinentalAdapterMath.Active() || CompilerVillageSelected()) return original;
		if (found && WorldGen.InWorld(original.X, original.Y, 32)) return original;
		Rectangle area = (Rectangle)corruptionRectangleField.GetValue(corruption);
		Rectangle village = (Rectangle)villageRectField.GetValue(null);
		int left = Math.Max(32, area.Left), right = Math.Min(Main.maxTilesX - 32, area.Right);
		int top = Math.Max(32, area.Top), bottom = Math.Min(Main.maxTilesY - 32, area.Bottom);
		var grass = (HashSet<int>)villageGrassField.GetValue(null);
		var stone = (HashSet<int>)villageStoneField.GetValue(null);
		bool Eligible(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			bool evilWall = tile.WallType is 3 or 83;
			return evilWall || tile.HasTile && Main.tileSolid[tile.TileType] &&
				(grass.Contains(tile.TileType) || stone.Contains(tile.TileType));
		}
		if (!SurfaceEntranceSearch.Find(left, top, right, bottom, village.Center.X,
			Eligible, out int x, out int y))
			throw new InvalidOperationException($"SOTS village has no physical entrance in {area}; randomFound={found}; last={original}.");
		DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_SOTS_VILLAGE_ENTRANCE|source=exhaustive-provider-predicate|randomFound={found}|last={original}|selected={x},{y}|evil={area}|village={village}");
		return new Point16(x, y);
	}
}
