using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
    // Both native rubble families use Style3x2. An old partial pile can clear
    // its entire old footprint directly during framing, bypassing KillTile.
    private static void ClearIntersectedRubble(Mod mod, StructureWritePlan plan, int width, int height, Point origin)
    {
        HashSet<(Point Anchor, ushort Type)> piles = new();
        for (int x = Math.Max(1, origin.X - 2); x < Math.Min(Main.maxTilesX - 1, origin.X + width + 2); x++)
        for (int y = Math.Max(1, origin.Y - 1); y < Math.Min(Main.maxTilesY - 1, origin.Y + height + 1); y++)
        {
            Tile tile = Main.tile[x,y];
            if (!tile.HasTile || tile.TileType is not (TileID.LargePiles or TileID.LargePiles2)) continue;
            Point anchor = new(x - tile.TileFrameX / 18 % 3, y - tile.TileFrameY / 18 % 2);
            for (int dx=0;dx<3;dx++) for(int dy=0;dy<2;dy++)
            {
                int sx=anchor.X+dx-origin.X, sy=anchor.Y+dy-origin.Y;
                if(sx>=0&&sy>=0&&sx<width&&sy<height&&plan.WriteTiles[sx*height+sy])piles.Add((anchor,tile.TileType));
            }
        }
        foreach(var pile in piles)
        for(int dx=0;dx<3;dx++) for(int dy=0;dy<2;dy++)
        {
            int x=pile.Anchor.X+dx,y=pile.Anchor.Y+dy;
            if(!WorldGen.InWorld(x,y))continue;
            Tile tile=Main.tile[x,y];if(tile.HasTile&&tile.TileType==pile.Type)tile.HasTile=false;
        }
        if(piles.Count>0)mod.Logger.Info($"DINGDONG_TERRAIN_OBJECT_CLEARANCE|kind=Rubble3x2|count={piles.Count}|origin={origin}");
    }

    // A partial OLD vanilla door calls CheckDoorClosed/Open during framing.
    // That native cleanup kills every cell of the old object, including newly
    // pasted masonry. Remove the complete intersected object before importing.
    // The admitted twelve-tile framing halo contains its maximum 2x3 footprint.
    private static void ClearIntersectedDoors(Mod mod, StructureWritePlan plan, int width, int height, Point origin)
    {
        HashSet<(Point Anchor, ushort Type)> doors = new();
        for (int x = Math.Max(1, origin.X - 1); x < Math.Min(Main.maxTilesX - 1, origin.X + width + 1); x++)
        for (int y = Math.Max(1, origin.Y - 2); y < Math.Min(Main.maxTilesY - 1, origin.Y + height + 2); y++)
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType is not (TileID.ClosedDoor or TileID.OpenDoor)) continue;
            int doorWidth = tile.TileType == TileID.OpenDoor ? 2 : 1;
            Point anchor = new(x - tile.TileFrameX / 18 % doorWidth, y - tile.TileFrameY / 18 % 3);
            for (int dx = 0; dx < doorWidth; dx++)
            for (int dy = 0; dy < 3; dy++)
            {
                int sx = anchor.X + dx - origin.X, sy = anchor.Y + dy - origin.Y;
                if (sx >= 0 && sy >= 0 && sx < width && sy < height && plan.WriteTiles[sx * height + sy])
                    doors.Add((anchor, tile.TileType));
            }
        }
        foreach (var door in doors)
        for (int dx = 0; dx < (door.Type == TileID.OpenDoor ? 2 : 1); dx++)
        for (int dy = 0; dy < 3; dy++)
        {
            int x = door.Anchor.X + dx, y = door.Anchor.Y + dy;
            if (!WorldGen.InWorld(x, y)) continue;
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && tile.TileType == door.Type) tile.HasTile = false;
        }
        if (doors.Count > 0) mod.Logger.Info($"DINGDONG_TERRAIN_OBJECT_CLEARANCE|kind=Door|count={doors.Count}|origin={origin}");
    }

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
