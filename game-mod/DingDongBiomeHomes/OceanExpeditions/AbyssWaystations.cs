using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static DingDongBiomeHomes.OceanExpeditions.OceanContentKit;

namespace DingDongBiomeHomes.OceanExpeditions;

// Roadmap item 7, Abyss side. The widened Calamity Abyss is crossed by Ding
// Dong's own water routes (FishingCalamityExpansion.FinishAbyss): winding
// shafts and a crossing about every 420 rows (V03 28.10: meandering, one shape
// shared with the carve, see AbyssRouteGeometry). On the owner's map these
// read as numbered empty floors leading nowhere. Each crossing now gets lit
// waystations (a slab with coral-torch posts; the first accepted one holds a
// depth-tiered cache), rock debris along the corridor and side caverns.
// Only empty water cells inside Ding Dong's route geometry receive tiles; the
// Calamity background walls the carve kept stay in place (SolidKeepWall), and
// nothing near a foreign chest or outside the routes is written. RangeFrame
// deliberately reframes the one-cell ring around each piece; the receipt
// reports measured type/wall changes of pre-existing cells in that ring.
internal static class AbyssWaystations
{
    // V03 28.10: FinishAbyss and this decor share one shape (AbyssRouteGeometry).
    private const int EdgeFrom = AbyssRouteGeometry.EdgeFrom, EdgeTo = AbyssRouteGeometry.EdgeTo, BottomMargin = AbyssRouteGeometry.BottomMargin;
    private const int StationHalf = AbyssRouteGeometry.StationHalf, StationHeight = 6, ChestClearance = 40;
    // V03 28.10: fixed sites lost stations over Calamity's chasm (no bed near
    // edge ~510) and native caves. Each preferred site now searches +-84 along
    // its crossing; up to three per floor, the first accepted one holds the
    // cache. The geometry's flat sites (beside each shaft, the middle) go first.
    private const int SearchSpan = 84, SearchStep = 6, StationsPerFloor = 3;
    // V03 28.10: off the flat sites the bed meanders; a slab levels at most this
    // many rows of slope, with a foundation down to each column's own bed.
    private const int MaxBedStep = 4;
    private enum Reject { None, Route, Shaft, Slope, Bed, Chest }
    private static int crossings;

    private static bool InRoute(int edge, int y, int top, int routeBottom) =>
        AbyssRouteGeometry.InRoute(edge, y, top, routeBottom, crossings);

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
        // V03 28.10: Calamity-only content (reached through its PlaceAbyss hook).
        if (!ModLoader.HasMod("CalamityMod")) return;
        int routeBottom = bottom - BottomMargin;
        int totalFloors = crossings = AbyssRouteGeometry.CrossingCount(top, routeBottom);
        int seed = (Main.ActiveWorldFileData?.Seed ?? 0) ^ 0x4AB55E70;
        ushort gravel = Find("AbyssGravel"), voidstone = Find("Voidstone");
        int stations = 0, chests = 0, rubble = 0, rejected = 0, onSite = 0;
        var fail = new int[(int)Reject.Chest + 1];
        var foreign = new ForeignChanges();
        var caverns = new AbyssCaverns.Receipt();
        bool isolated = IsolatedGenRand.Run(seed, () =>
        {
            var random = new Random(seed);
            var accepted = new List<int>(StationsPerFloor);
            for (int k = 0; k < totalFloors; k++)
            {
                accepted.Clear();
                bool cacheWanted = true;
                int[] preferred = { AbyssRouteGeometry.Site(top, k, 0), AbyssRouteGeometry.Site(top, k, 2), AbyssRouteGeometry.MiddleSite, 330, 680 };
                foreach (int site in preferred)
                {
                    if (accepted.Count == StationsPerFloor) break;
                    for (int i = 0; i <= SearchSpan / SearchStep * 2; i++)
                    {
                        int edge = site + (i + 1) / 2 * SearchStep * (i % 2 == 1 ? 1 : -1);
                        if (edge - StationHalf < EdgeFrom || edge + StationHalf >= EdgeTo) continue;
                        bool clear = true;
                        foreach (int a in accepted) if (Math.Abs(a - edge) < StationHalf * 2 + 24) clear = false;
                        if (!clear) continue;
                        Reject why = TryStation(left, top, routeBottom, k, edge, cacheWanted, random, foreign, out bool chest);
                        if (why != Reject.None) { fail[(int)why]++; continue; }
                        accepted.Add(edge); stations++;
                        if (i == 0) onSite++;
                        if (chest) { chests++; cacheWanted = false; }
                        break;
                    }
                }
                rejected += StationsPerFloor - accepted.Count;
                ushort material = k * 2 >= totalFloors ? voidstone : gravel;
                if (material != 0) rubble += Debris(left, top, routeBottom, k, accepted, material, random, foreign);
            }
            // V03 28.10: side caverns along the shafts (separate stream: the
            // station/debris sequence above stays independent of it).
            AbyssCaverns.Generate(left, top, routeBottom, crossings, new Random(seed ^ 0x0CA7E5), caverns);
        });
        mod.Logger.Info($"DINGDONG_ABYSS_WAYSTATIONS|side={(left ? "left" : "right")}|top={top}|bottom={bottom}|floors={totalFloors}|stations={stations}/{totalFloors * StationsPerFloor}|stationsOnSite={onSite}|lootChests={chests}|debris={rubble}|rejectedStations={rejected}|stationProbeFail=route:{fail[(int)Reject.Route]},shaft:{fail[(int)Reject.Shaft]},slope:{fail[(int)Reject.Slope]},bed:{fail[(int)Reject.Bed]},chest:{fail[(int)Reject.Chest]}|genRandIsolated={isolated}|foreignTilesChanged={foreign.Tiles}|foreignWallsChanged={foreign.Walls}|cellsWritten={foreign.Written}|wallsKept={foreign.WallsKept}|{caverns}");
    }

    private static ushort Find(string name) =>
        ModContent.TryFind<ModTile>("CalamityMod/" + name, out var tile) ? (ushort)tile.Type : (ushort)0;

    private static Reject TryStation(bool left, int top, int routeBottom, int tier, int edge, bool cache,
        Random random, ForeignChanges foreign, out bool chestPlaced)
    {
        chestPlaced = false;
        // V03 28.10: per-column bed of the meandering crossing (level on the
        // geometry's sites). The slab top rests on the highest bed cell; lower
        // columns get a foundation down to their own bed.
        Span<int> bed = stackalloc int[StationHalf * 2 + 1];
        int high = int.MaxValue, low = int.MinValue;
        for (int i = 0; i < bed.Length; i++)
        {
            bed[i] = AbyssRouteGeometry.Floor(top, tier, edge - StationHalf + i);
            high = Math.Min(high, bed[i]); low = Math.Max(low, bed[i]);
        }
        if (low - high > MaxBedStep) return Reject.Slope;
        int yTop = high - StationHeight + 1;
        int supported = 0;
        for (int i = 0; i < bed.Length; i++)
        {
            int e = edge - StationHalf + i;
            for (int y = yTop; y <= bed[i]; y++)
                if (!Owned(left, e, y, top, routeBottom)) return Reject.Route;
            // Never plug a shaft that continues below the crossing.
            if (AbyssRouteGeometry.InShaft(e, bed[i] + 1, top, crossings)) return Reject.Shaft;
            if (Supported(left, e, bed[i] + 1)) supported++;
        }
        // A slab rests on the corridor bed; never float one over an open chasm.
        if (supported * 10 < bed.Length * 7) return Reject.Bed;
        int cx = X(left, edge);
        if (!NoChestNear(cx, high, ChestClearance)) return Reject.Chest;
        Rectangle area = Area(left, edge - StationHalf, edge + StationHalf, yTop, low);
        CellState[] before = Snapshot(area);
        for (int i = 0; i < bed.Length; i++)
        {
            int x = X(left, edge - StationHalf + i);
            for (int y = high - 1; y <= bed[i]; y++) Write(foreign, x, y, TileID.CopperPlating);
        }
        foreach (int post in new[] { edge - StationHalf + 1, edge + StationHalf - 1 })
        {
            int x = X(left, post);
            for (int y = high - 2; y >= high - 3; y--) Write(foreign, x, y, TileID.CopperPlating);
            WorldGen.PlaceTile(x, high - 4, TileID.Torches, mute: true, forced: true, style: TorchID.Coral);
        }
        if (cache)
        {
            int x = Math.Min(cx, X(left, edge + 1));
            chestPlaced = LootChest(x, high - 1, TileID.CopperPlating, 17, new (int, int)[]
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
        return Reject.None;
    }

    // Rock mounds on the corridor bed and short stalactites under its ceiling,
    // spaced along the crossing away from the shafts and the stations.
    private static int Debris(bool left, int top, int routeBottom, int k, List<int> stations,
        ushort material, Random random, ForeignChanges foreign)
    {
        int placed = 0;
        for (int edge = 100 + random.Next(40); edge < EdgeTo - 40; edge += 56 + random.Next(30))
        {
            // V03 28.10: meandering crossing; bed and roof are read per column.
            int floorRow = AbyssRouteGeometry.Floor(top, k, edge), ceiling = AbyssRouteGeometry.Ceiling(k, edge, floorRow);
            if (AbyssRouteGeometry.InShaft(edge - 16, floorRow, top, crossings) || AbyssRouteGeometry.InShaft(edge + 16, floorRow, top, crossings)) continue;
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
