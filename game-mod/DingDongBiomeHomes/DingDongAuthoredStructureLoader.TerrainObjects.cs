using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
    // Vanilla Check3x2 forcibly reconstructs a partial FallenLog during worldgen,
    // including its grass support. Remove intersected OLD logs atomically before
    // the source is pasted; do not let framing overwrite the new masonry.
    private static void ClearIntersectedFallenLogs(Mod mod, StructureWritePlan plan, int width, int height, Point origin)
    {
        HashSet<Point> logs = new();
        for (int x = Math.Max(0, origin.X - 2); x < Math.Min(Main.maxTilesX, origin.X + width + 2); x++)
        for (int y = Math.Max(0, origin.Y - 1); y < Math.Min(Main.maxTilesY, origin.Y + height + 1); y++)
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType != TileID.FallenLog) continue;
            Point anchor = new(x - tile.TileFrameX / 18 % 3, y - tile.TileFrameY / 18 % 2);
            for (int dx = 0; dx < 3; dx++)
            for (int dy = 0; dy < 2; dy++)
            {
                int lx = anchor.X + dx - origin.X, ly = anchor.Y + dy - origin.Y;
                if (lx >= 0 && ly >= 0 && lx < width && ly < height && plan.WriteTiles[lx * height + ly])
                    logs.Add(anchor);
            }
        }
        foreach (Point anchor in logs)
        for (int dx = 0; dx < 3; dx++)
        for (int dy = 0; dy < 2; dy++)
        {
            int x = anchor.X + dx, y = anchor.Y + dy;
            if (!WorldGen.InWorld(x, y)) continue;
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && tile.TileType == TileID.FallenLog) tile.HasTile = false;
        }
        if (logs.Count > 0) mod.Logger.Info($"DINGDONG_TERRAIN_OBJECT_CLEARANCE|kind=FallenLog|count={logs.Count}|origin={origin}");
    }
}
