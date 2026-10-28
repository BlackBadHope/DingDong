using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static DingDongBiomeHomes.OceanExpeditions.OceanContentKit;

namespace DingDongBiomeHomes.OceanExpeditions;

// Roadmap item 7, Abyss side. The widened Calamity Abyss is crossed by Ding
// Dong's own water routes (FishingCalamityExpansion.FinishAbyss): two winding
// shafts and a horizontal crossing every 420 rows. On the owner's map these
// read as numbered empty floors leading nowhere. Each crossing now gets lit
// waystations (a slab with coral-torch posts; the middle one holds a
// depth-tiered cache) and rock debris along the corridor.
// Only empty water cells inside Ding Dong's route geometry receive tiles; the
// Calamity background walls the carve kept stay in place (SolidKeepWall), and
// nothing near a foreign chest or outside the routes is written. RangeFrame
// deliberately reframes the one-cell ring around each piece; the receipt
// reports measured type/wall changes of pre-existing cells in that ring.
internal static class AbyssWaystations
{
    // Must mirror FinishAbyss exactly.
    private const int Pitch = 420, CrossingHeight = 46, EdgeFrom = 70, EdgeTo = 960, BottomMargin = 70;
    private const int StationHalf = 10, StationHeight = 6, ChestClearance = 40;

    private static int Offset(int y) => (int)(Math.Sin(y * 0.006) * 30);

    private static bool InRoute(int edge, int y, int top, int routeBottom)
    {
        if (edge < EdgeFrom || edge >= EdgeTo || y < top || y >= routeBottom) return false;
        int offset = Offset(y);
        bool shaft = Math.Abs(edge - (180 + offset)) < 44 || Math.Abs(edge - (820 - offset)) < 48;
        bool crossing = (y - top) % Pitch is >= 0 and < CrossingHeight;
        return shaft || crossing;
    }

    private static int X(bool left, int edge) => left ? edge : Main.maxTilesX - edge;

    // Owned: inside the route geometry, still empty water after the carve.
    private static bool Owned(bool left, int edge, int y, int top, int routeBottom)
    {
        if (!InRoute(edge, y, top, routeBottom)) return false;
        int x = X(left, edge);
        if (x < 1 || x >= Main.maxTilesX - 1) return false;
        Tile t = Main.tile[x, y];
        return !t.HasTile && t.LiquidAmount >= 200 && t.LiquidType == LiquidID.Water;
    }

    private static bool Supported(bool left, int edge, int y)
    {
        int x = X(left, edge);
        Tile t = Main.tile[x, y];
        return t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
    }

    private static Rectangle Area(bool left, int edgeFrom, int edgeTo, int yFrom, int yTo)
    {
        int a = X(left, edgeFrom), b = X(left, edgeTo);
        return new Rectangle(Math.Min(a, b), yFrom, Math.Abs(b - a) + 1, yTo - yFrom + 1);
    }

    internal static void Decorate(Mod mod, bool left, int top, int bottom)
    {
        int routeBottom = bottom - BottomMargin;
        int totalFloors = 0;
        for (int k = 0; top + Pitch * k + CrossingHeight - 1 < routeBottom; k++) totalFloors++;
        int seed = (Main.ActiveWorldFileData?.Seed ?? 0) ^ 0x4AB55E70;
        ushort gravel = Find("AbyssGravel"), voidstone = Find("Voidstone");
        int stations = 0, chests = 0, rubble = 0, rejected = 0;
        var foreign = new ForeignChanges();
        bool isolated = IsolatedGenRand.Run(seed, () =>
        {
            var random = new Random(seed);
            for (int k = 0; k < totalFloors; k++)
            {
                int ceiling = top + Pitch * k, floorRow = ceiling + CrossingHeight - 1;
                int offset = Offset(floorRow);
                int[] sites = { 180 + offset + 44 + 14, 500, 820 - offset - 48 - 14 };
                // The cache prefers the middle station; if that one is rejected
                // (or its chest fails) it moves to the next accepted station.
                bool cacheWanted = true;
                foreach (int s in new[] { 1, 0, 2 })
                {
                    if (!TryStation(left, top, routeBottom, sites[s], floorRow, cacheWanted, k, random, foreign, out bool chest)) { rejected++; continue; }
                    stations++;
                    if (chest) { chests++; cacheWanted = false; }
                }
                ushort material = k * 2 >= totalFloors ? voidstone : gravel;
                if (material != 0) rubble += Debris(left, top, routeBottom, ceiling, floorRow, sites, material, random, foreign);
            }
        });
        mod.Logger.Info($"DINGDONG_ABYSS_WAYSTATIONS|side={(left ? "left" : "right")}|top={top}|bottom={bottom}|floors={totalFloors}|stations={stations}/{totalFloors * 3}|lootChests={chests}|debris={rubble}|rejectedStations={rejected}|genRandIsolated={isolated}|foreignTilesChanged={foreign.Tiles}|foreignWallsChanged={foreign.Walls}|cellsWritten={foreign.Written}|wallsKept={foreign.WallsKept}");
    }

    private static ushort Find(string name) =>
        ModContent.TryFind<ModTile>("CalamityMod/" + name, out var tile) ? (ushort)tile.Type : (ushort)0;

    private static bool TryStation(bool left, int top, int routeBottom, int edge, int floorRow, bool cache, int tier,
        Random random, ForeignChanges foreign, out bool chestPlaced)
    {
        chestPlaced = false;
        int yTop = floorRow - StationHeight + 1;
        int supported = 0;
        for (int e = edge - StationHalf; e <= edge + StationHalf; e++)
        {
            for (int y = yTop; y <= floorRow; y++)
                if (!Owned(left, e, y, top, routeBottom)) return false;
            if (Supported(left, e, floorRow + 1)) supported++;
        }
        // A slab rests on the corridor bed; never float one over an open chasm.
        if (supported * 10 < (StationHalf * 2 + 1) * 7) return false;
        int cx = X(left, edge);
        if (!NoChestNear(cx, floorRow, ChestClearance)) return false;
        Rectangle area = Area(left, edge - StationHalf, edge + StationHalf, yTop, floorRow);
        CellState[] before = Snapshot(area);
        for (int e = edge - StationHalf; e <= edge + StationHalf; e++)
        {
            int x = X(left, e);
            Write(foreign, x, floorRow, TileID.CopperPlating);
            Write(foreign, x, floorRow - 1, TileID.CopperPlating);
        }
        foreach (int post in new[] { edge - StationHalf + 1, edge + StationHalf - 1 })
        {
            int x = X(left, post);
            for (int y = floorRow - 2; y >= floorRow - 3; y--) Write(foreign, x, y, TileID.CopperPlating);
            WorldGen.PlaceTile(x, floorRow - 4, TileID.Torches, mute: true, forced: true, style: TorchID.Coral);
        }
        if (cache)
        {
            int x = Math.Min(cx, X(left, edge + 1));
            chestPlaced = LootChest(x, floorRow - 1, TileID.CopperPlating, 17, new (int, int)[]
            {
                (ModContent.ItemType<HadalLens>(), 1 + tier / 3), (ItemID.GoldCoin, 1 + tier / 2),
                (ItemID.GillsPotion, 3), (ItemID.ShinePotion, 2), (ItemID.NightOwlPotion, 2),
                tier >= 3 ? (ItemID.SpelunkerPotion, 2) : (ItemID.Glowstick, 10 + random.Next(6)),
                (ModContent.ItemType<PelagicScale>(), 2)
            }, keepWall: true) >= 0;
        }
        WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
        Flood(area);
        foreign.Count(area, before);
        // Not registered as a protected structure: no later pass writes the Abyss
        // routes, and registering would grow the integrity guard's capture.
        return true;
    }

    // Rock mounds on the corridor bed and short stalactites under its ceiling,
    // spaced along the crossing away from the shafts and the stations.
    private static int Debris(bool left, int top, int routeBottom, int ceiling, int floorRow, int[] stations,
        ushort material, Random random, ForeignChanges foreign)
    {
        int placed = 0, offset = Offset(floorRow);
        for (int edge = 100 + random.Next(40); edge < EdgeTo - 40; edge += 56 + random.Next(30))
        {
            if (Math.Abs(edge - (180 + offset)) < 60 || Math.Abs(edge - (820 - offset)) < 64) continue;
            bool nearStation = false;
            foreach (int s in stations) if (Math.Abs(edge - s) < StationHalf + 12) nearStation = true;
            if (nearStation) continue;
            bool hanging = random.Next(2) == 0;
            int size = 2 + random.Next(3);
            int anchor = hanging ? ceiling - 1 : floorRow + 1;
            // The whole footprint must rest on (or hang from) rock, never over a void.
            bool supported = true;
            for (int dx = -size; dx <= size && supported; dx++) supported = Supported(left, edge + dx, anchor);
            if (!supported) continue;
            bool owned = true;
            for (int dx = -size; dx <= size && owned; dx++)
            for (int i = 0; i < size - Math.Abs(dx) + 1 && owned; i++)
                owned = Owned(left, edge + dx, hanging ? ceiling + i : floorRow - i, top, routeBottom);
            if (!owned) continue;
            Rectangle area = Area(left, edge - size, edge + size, hanging ? ceiling : floorRow - size, hanging ? ceiling + size : floorRow);
            CellState[] before = Snapshot(area);
            for (int dx = -size; dx <= size; dx++)
            for (int i = 0; i < size - Math.Abs(dx) + 1; i++)
                Write(foreign, X(left, edge + dx), hanging ? ceiling + i : floorRow - i, material);
            WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
            foreign.Count(area, before);
            placed++;
        }
        return placed;
    }
    private static void Write(ForeignChanges foreign, int x, int y, ushort type)
    {
        foreign.Written++;
        if (Main.tile[x, y].WallType != 0) foreign.WallsKept++;
        SolidKeepWall(x, y, type);
    }

    private struct CellState
    {
        public bool HasTile;
        public ushort TileType, WallType;
    }

    // RangeFrame(l, t, r, b) frames x in [l-1, r+1] and y in [t-1, b+1]; the
    // snapshot covers exactly that ring plus the area (clamped to the world).
    private static (int X0, int Y0, int X1, int Y1) Ring(Rectangle area) =>
        (Math.Max(0, area.Left - 1), Math.Max(0, area.Top - 1),
         Math.Min(Main.maxTilesX - 1, area.Right + 1), Math.Min(Main.maxTilesY - 1, area.Bottom + 1));

    private static CellState[] Snapshot(Rectangle area)
    {
        var (x0, y0, x1, y1) = Ring(area);
        var cells = new CellState[(x1 - x0 + 1) * (y1 - y0 + 1)];
        int i = 0;
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++, i++)
        {
            Tile t = Main.tile[x, y];
            cells[i] = new CellState { HasTile = t.HasTile, TileType = t.TileType, WallType = t.WallType };
        }
        return cells;
    }

    // Counts pre-existing tiles that vanished or changed type and any wall that
    // changed; empty water cells becoming Ding Dong tiles are the intended writes.
    private sealed class ForeignChanges
    {
        public int Tiles, Walls, Written, WallsKept;

        public void Count(Rectangle area, CellState[] before)
        {
            var (x0, y0, x1, y1) = Ring(area);
            int i = 0;
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++, i++)
            {
                Tile t = Main.tile[x, y];
                CellState b = before[i];
                if (b.HasTile && (!t.HasTile || t.TileType != b.TileType)) Tiles++;
                if (t.WallType != b.WallType) Walls++;
            }
        }
    }
}
