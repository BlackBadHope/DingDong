using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static DingDongBiomeHomes.OceanExpeditions.OceanContentKit;

namespace DingDongBiomeHomes.OceanExpeditions;

// V03 28.10: owner: "abyss is empty again, 3 shafts down". Between crossings
// (about every 420 rows) Ding Dong's shafts and Calamity's own chasm were bare
// water columns. Side caverns now branch off all three. A cavern is carved only
// out of Calamity abyss/sulphur stone: any foreign tile, chest or structure in
// its envelope rejects it, shaft cells are never written, walls are kept (biome
// visuals and Calamity spawns unchanged). Each cavern gets Calamity's own abyss
// flora, coral torches (Lumenyl crystals in the deep layers), plus one feature: a shrine with a
// depth-tiered cache, a bubble-sealed air pocket, or a gem/ore vein.
// Runs only from FinishAbyss (Calamity hook) inside IsolatedGenRand.
internal static class AbyssCaverns
{
    internal sealed class Receipt
    {
        public int Anchors, Caverns, Rejected, NearCrossing, Cells, Shrines, ShrineChests, AirPockets, Veins, VeinCells, Flora, Lumenyl, Lights, ScatterAnchors, Scattered;

        public override string ToString() =>
            $"caverns={Caverns}|cavernAnchors={Anchors}|cavernsRejected={Rejected}|cavernsNearCrossing={NearCrossing}|cavernCells={Cells}|shrines={Shrines}|shrineChests={ShrineChests}" +
            $"|airPockets={AirPockets}|veins={Veins}|veinCells={VeinCells}|flora={Flora}|lumenyl={Lumenyl}|lights={Lights}|scatterAnchors={ScatterAnchors}|scattered={Scattered}";
    }

    // Biome X gate is edge < 930 (FishingCalamityExpansion); keep a margin.
    private const int EdgeFrom = AbyssRouteGeometry.EdgeFrom, EdgeMax = 920;
    // V03 28.10: clearance between a cavern envelope and any crossing cell.
    private const int CrossingPad = 12;

    private sealed class Context
    {
        public bool Left;
        public int Top, RouteBottom, Crossings;
        public bool[] Carvable;
        public ushort Shale, Gravel, Pyre, Void, Lumenyl, SmoothGravel, SmoothVoid;
        public ushort[][] Flora;
        public List<Rectangle> Placed = new();
        public Random Random;
        public Receipt R;
    }

    private static int X(Context c, int edge) => c.Left ? edge : Main.maxTilesX - edge;
    private static Tile Cell(Context c, int edge, int y) => Main.tile[X(c, edge), y];
    private static bool Inside(double dx, double dy, double a, double b) => dx * dx / (a * a) + dy * dy / (b * b) <= 1;
    private static bool Solid(Tile t) => t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];

    private static ushort Find(string name) =>
        ModContent.TryFind<ModTile>("CalamityMod/" + name, out var tile) ? (ushort)tile.Type : (ushort)0;

    private static int ItemType(string name) =>
        ModContent.TryFind<ModItem>("CalamityMod/" + name, out var item) ? item.Type : 0;

    private static ushort[] Types(params string[] names)
    {
        var list = new List<ushort>();
        foreach (string name in names) { ushort t = Find(name); if (t != 0) list.Add(t); }
        return list.ToArray();
    }

    internal static void Generate(bool left, int top, int routeBottom, int crossings, Random random, Receipt receipt)
    {
        var c = new Context { Left = left, Top = top, RouteBottom = routeBottom, Crossings = crossings, Random = random, R = receipt };
        c.Shale = Find("SulphurousShale"); c.Gravel = Find("AbyssGravel"); c.Pyre = Find("PyreMantle"); c.Void = Find("Voidstone");
        c.Lumenyl = Find("LumenylCrystals"); c.SmoothGravel = Find("SmoothAbyssGravel"); c.SmoothVoid = Find("SmoothVoidstone");
        c.Carvable = new bool[TileLoader.TileCount];
        foreach (ushort t in Types("SulphurousShale", "AbyssGravel", "PyreMantle", "Voidstone", "SulphurousSand", "SulphurousSandstone", "HardenedSulphurousSandstone"))
            c.Carvable[t] = true;
        if (c.Shale == 0 || c.Gravel == 0 || c.Pyre == 0 || c.Void == 0) return;
        c.Flora = new[]
        {
            Types("SulphurTubeCoral", "SulphurTentacleCorals", "SulphurPireCoral1", "SulphurPireCoral2", "SulphurPireCoral3", "ShalePile1", "ShalePile2", "ShalePile3"),
            Types("GravelPile1", "GravelPile2", "GravelPile3", "SpiderCoral1", "SpiderCoral2", "SpiderCoral3", "PlantyMushPile1", "PlantyMushPile2", "BulbTree1"),
            Types("BulbTree1", "BulbTree2", "BulbTree3", "PhoviamareHalm", "SpiderCoral4", "SpiderCoral5", "GravelPile3"),
            Types("AbyssGiantKelp1", "AbyssGiantKelp2", "AbyssGiantKelp3", "AbyssGiantKelp4", "PhoviamareHalm", "BulbTree2", "BulbTree3"),
        };
        // Anchors along both Ding Dong shafts and Calamity's chasm, never within
        // reach of a crossing (stations and debris live there).
        for (int shaft = 0; shaft < 3; shaft++)
        for (int y = top + 80 + random.Next(40); y < routeBottom - 60; y += 92 + random.Next(44))
        {
            int rx = 26 + random.Next(19), ry = 12 + random.Next(9);
            int feature = random.Next(10);
            int prefer = shaft == 1 ? -1 : random.Next(2) == 0 ? 1 : -1;
            // V03 28.10: meandering route. Shaft centres follow AbyssRouteGeometry;
            // a serpentine shaft exists only on some spans, so skip rows without it.
            int center = shaft switch
            {
                0 => 180 + AbyssRouteGeometry.ShaftOffset(y),
                1 => 820 - AbyssRouteGeometry.ShaftOffset(y),
                _ => ChasmCenter(c, y),
            };
            if (center < 0 || shaft < 2 && !AbyssRouteGeometry.InShaft(center, y, top, c.Crossings)) continue;
            // Crossings no longer sit in a fixed 46-row band: test real crossing cells.
            if (NearCrossing(c, center - 60, center + 60, y - ry - CrossingPad, y + ry + CrossingPad)) { c.R.NearCrossing++; continue; }
            c.R.Anchors++;
            if (!TryCavern(c, center, y, prefer, rx, ry, feature) && !TryCavern(c, center, y, -prefer, rx, ry, feature)
                && !TryCavern(c, center, y, prefer, rx * 3 / 4, Math.Max(10, ry * 3 / 4), feature))
                c.R.Rejected++;
        }
        // Owner, 29.10: "resources only along the way down - they must be everywhere". A grid over
        // the whole Abyss band: from the nearest natural water cell a side cavern branches into the
        // rock, with a vein (half), an air pocket or a shrine, plus flora and lights.
        for (int y = top + 70 + random.Next(30); y < routeBottom - 60; y += 70 + random.Next(30))
        for (int e = EdgeFrom + 40 + random.Next(40); e < EdgeMax - 40; e += 110 + random.Next(40))
        {
            int start = -1;
            for (int k = 0; k < 60 && start < 0; k += 3)
            {
                if (Water(Cell(c, e + k, y)) && !InRoute(c, e + k, y)) start = e + k;
                else if (Water(Cell(c, e - k, y)) && !InRoute(c, e - k, y)) start = e - k;
            }
            if (start < 0) continue;
            c.R.ScatterAnchors++;
            int rx = 18 + random.Next(14), ry = 9 + random.Next(7), feature = random.Next(2, 12);
            int d = random.Next(2) == 0 ? 1 : -1;
            if (TryCavern(c, start, y, d, rx, ry, feature) || TryCavern(c, start, y, -d, rx, ry, feature)) c.R.Scattered++;
        }
    }

    // Calamity's chasm is the widest open-water run near edge ~510 (native x3).
    private static int ChasmCenter(Context c, int y)
    {
        int found = -1;
        for (int d = 0; d <= 200 && found < 0; d += 4)
        {
            if (Water(Cell(c, 510 + d, y))) found = 510 + d;
            else if (Water(Cell(c, 510 - d, y))) found = 510 - d;
        }
        if (found < 0) return -1;
        int a = found, b = found;
        while (a > 300 && Water(Cell(c, a - 1, y))) a--;
        while (b < 720 && Water(Cell(c, b + 1, y))) b++;
        return b - a >= 24 ? (a + b) / 2 : -1;
    }

    private static bool Water(Tile t) => !t.HasTile && t.LiquidAmount >= 200 && t.LiquidType == LiquidID.Water;

    private static bool InRoute(Context c, int e, int y) => AbyssRouteGeometry.InRoute(e, y, c.Top, c.RouteBottom, c.Crossings);

    // V03 28.10: any crossing cell (route minus shaft) in the box, sampled every
    // 3 cells (a crossing is >= 31 rows tall; the pad covers the column step).
    private static bool NearCrossing(Context c, int e0, int e1, int y0, int y1)
    {
        for (int e = e0; e <= e1; e += 3)
        for (int y = y0; y <= y1; y += 3)
            if (InRoute(c, e, y) && !AbyssRouteGeometry.InShaft(e, y, c.Top, c.Crossings)) return true;
        return false;
    }

    private static bool TryCavern(Context c, int center, int y, int d, int rx, int ry, int feature)
    {
        // Wall: first solid cell outward from the open column; only clean water before it.
        int wall = -1;
        for (int e = center; Math.Abs(e - center) < 170; e += d)
        {
            if (e < EdgeFrom || e >= EdgeMax) return false;
            Tile t = Cell(c, e, y);
            if (!t.HasTile) { if (t.LiquidType != LiquidID.Water) return false; continue; }
            if (!Solid(t) || !c.Carvable[t.TileType]) return false;
            wall = e; break;
        }
        if (wall < 0) return false;
        int cx = wall + d * (rx - 8);
        int e0 = cx - rx - 2, e1 = cx + rx + 2, y0 = y - ry - 2, y1 = y + ry + 2;
        if (e0 < EdgeFrom || e1 >= EdgeMax || y0 < c.Top + 20 || y1 > c.RouteBottom) return false;
        if (NearCrossing(c, e0 - CrossingPad, e1 + CrossingPad, y0 - CrossingPad, y1 + CrossingPad)) return false;
        var box = new Rectangle(e0, y0, e1 - e0 + 1, y1 - y0 + 1);
        Rectangle padded = box; padded.Inflate(12, 12);
        foreach (Rectangle p in c.Placed) if (p.Intersects(padded)) return false;
        if (!NoChestNear(X(c, cx), y, rx + 24)) return false;
        for (int e = e0; e <= e1; e++)
        for (int yy = y0; yy <= y1; yy++)
        {
            if (!Inside(e - cx, yy - y, rx + 1.5, ry + 1.5)) continue;
            Tile t = Cell(c, e, yy);
            if (t.HasTile ? !c.Carvable[t.TileType] : t.LiquidAmount > 0 && t.LiquidType != LiquidID.Water) return false;
        }
        int tier = Tier(c, Cell(c, wall, y).TileType, y);
        for (int e = e0; e <= e1; e++)
        for (int yy = y0; yy <= y1; yy++)
        {
            if (!Inside(e - cx, yy - y, rx, ry)) continue;
            Tile t = Cell(c, e, yy);
            if (t.HasTile) { t.ClearTile(); c.R.Cells++; }
            t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
        }
        c.Placed.Add(box);
        Rectangle world = World(c, e0, e1, y0, y1);
        WorldGen.RangeFrame(world.Left, world.Top, world.Right, world.Bottom);
        c.R.Caverns++;

        bool shrine = feature <= 3 && Shrine(c, cx, y, d, rx, ry, tier);
        Decorate(c, cx, y, d, rx, ry, tier);
        Flood(world);
        // The air pocket is dug after the flood: its dry room must stay dry.
        if (!shrine && !(feature is >= 4 and <= 6 && AirPocket(c, cx, y, d, rx))) Vein(c, cx, y, d, rx, ry, tier);
        return true;
    }

    private static Rectangle World(Context c, int e0, int e1, int y0, int y1)
    {
        int a = X(c, e0), b = X(c, e1);
        return new Rectangle(Math.Min(a, b), y0, Math.Abs(b - a) + 1, y1 - y0 + 1);
    }

    private static int Tier(Context c, ushort wall, int y)
    {
        if (wall == c.Gravel) return 2;
        if (wall == c.Pyre) return 3;
        if (wall == c.Void) return 4;
        if (wall == c.Shale) return 1;
        return Math.Clamp(1 + 4 * (y - c.Top) / Math.Max(1, c.RouteBottom - c.Top), 1, 4);
    }

    // Cell carved for this cavern, away from the shaft side and any route.
    // Slack +0.5 admits the carved rim cells resting on the cavern bed.
    private static bool Owned(Context c, int cx, int y, int d, int rx, int ry, int e, int yy)
    {
        if (!Inside(e - cx, yy - y, rx + 0.5, ry + 0.5) || d * (e - cx) < -rx / 2) return false;
        if (InRoute(c, e, yy)) return false;
        return Water(Cell(c, e, yy));
    }

    private static int FloorBelow(Context c, int e, int y, int ry)
    {
        for (int yy = y; yy <= y + ry + 3; yy++)
            if (Solid(Cell(c, e, yy))) return yy;
        return -1;
    }

    private static int CeilingAbove(Context c, int e, int y, int ry)
    {
        for (int yy = y; yy >= y - ry - 3; yy--)
            if (Solid(Cell(c, e, yy))) return yy;
        return -1;
    }

    private static void Decorate(Context c, int cx, int y, int d, int rx, int ry, int tier)
    {
        ushort[] flora = c.Flora[tier - 1];
        int from = cx - d * (rx / 2), to = cx + d * (rx - 2);
        for (int e = from; d * (to - e) >= 0; e += d * (4 + c.Random.Next(4)))
        {
            int floor = FloorBelow(c, e, y, ry);
            if (floor > 0 && Owned(c, cx, y, d, rx, ry, e, floor - 1) && flora.Length > 0)
            {
                ushort type = flora[c.Random.Next(flora.Length)];
                int x = X(c, e);
                WorldGen.PlaceObject(x, floor - 1, type, mute: true);
                if (Placed(x, floor - 1, type)) c.R.Flora++;
            }
            int ceiling = CeilingAbove(c, e, y, ry);
            // Mined Lumenyl is a post-Calamitas material: crystals only in the
            // two deep layers, which already demand late gear to survive.
            if (tier >= 3 && c.Lumenyl != 0 && ceiling > 0 && c.Random.Next(3) > 0 && Owned(c, cx, y, d, rx, ry, e, ceiling + 1))
            {
                int x = X(c, e);
                WorldGen.PlaceTile(x, ceiling + 1, c.Lumenyl, mute: true);
                if (Main.tile[x, ceiling + 1].HasTile && Main.tile[x, ceiling + 1].TileType == c.Lumenyl) c.R.Lumenyl++;
            }
        }
        // Guaranteed lights: two coral torches (they burn under water) on the bed.
        foreach (int le in new[] { cx - d * (rx / 4), cx + d * (rx * 2 / 3) })
        {
            int lf = FloorBelow(c, le, y, ry);
            if (lf <= 0 || !Owned(c, cx, y, d, rx, ry, le, lf - 1)) continue;
            int x = X(c, le);
            WorldGen.PlaceTile(x, lf - 1, TileID.Torches, mute: true, forced: true, style: TorchID.Coral);
            if (Main.tile[x, lf - 1].HasTile && Main.tile[x, lf - 1].TileType == TileID.Torches) c.R.Lights++;
        }
    }

    // Multi-tile objects anchor by their own origin; look around the anchor.
    private static bool Placed(int x, int y, ushort type)
    {
        for (int i = Math.Max(1, x - 2); i <= Math.Min(Main.maxTilesX - 2, x + 2); i++)
        for (int j = y - 4; j <= y; j++)
            if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == type) return true;
        return false;
    }

    // Ruin on the cavern bed: flat slab, two pillars with a broken lintel,
    // coral torches and a depth-tiered water chest. All cells must be this
    // cavern's own carved water.
    private static bool Shrine(Context c, int cx, int y, int d, int rx, int ry, int tier)
    {
        int se = cx + d * (rx / 3);
        int minFloor = int.MaxValue;
        Span<int> floors = stackalloc int[15];
        for (int i = 0; i < 15; i++)
        {
            int f = FloorBelow(c, se - 7 + i, y, ry);
            if (f < 0) return false;
            floors[i] = f; minFloor = Math.Min(minFloor, f);
        }
        int s = minFloor - 1;
        for (int i = 0; i < 15; i++)
        {
            int e = se - 7 + i;
            for (int yy = s - 8; yy < floors[i]; yy++)
                if (!Owned(c, cx, y, d, rx, ry, e, yy)) return false;
        }
        ushort material = tier <= 2 ? c.SmoothGravel : c.SmoothVoid;
        if (material == 0) material = TileID.CopperPlating;
        for (int i = 0; i < 15; i++)
            for (int yy = s; yy < floors[i]; yy++) SolidKeepWall(X(c, se - 7 + i), yy, material);
        foreach (int pe in new[] { se - 6, se + 6 })
            for (int yy = s - 7; yy < s; yy++) SolidKeepWall(X(c, pe), yy, material);
        for (int i = 0; i < 15; i++)
            if (Math.Abs(i - 7) >= 4) SolidKeepWall(X(c, se - 7 + i), s - 8, material);
        c.R.Shrines++;
        Rectangle area = World(c, se - 7, se + 7, s - 8, s + 8);
        WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
        foreach (int te in new[] { se - 3, se + 4 })
            WorldGen.PlaceTile(X(c, te), s - 1, TileID.Torches, mute: true, forced: true, style: TorchID.Coral);
        int chestX = Math.Min(X(c, se), X(c, se + 1));
        if (LootChest(chestX, s, material, 17, Loot(c, tier), keepWall: true) >= 0) c.R.ShrineChests++;
        return true;
    }

    private static (int, int)[] Loot(Context c, int tier)
    {
        Random r = c.Random;
        bool platinum = WorldGen.SavedOreTiers.Gold == TileID.Platinum;
        int bar = platinum ? ItemID.PlatinumBar : ItemID.GoldBar;
        int scale = ItemType("SulphuricScale");
        int[] gems = { ItemID.Amethyst, ItemID.Topaz, ItemID.Sapphire, ItemID.Emerald, ItemID.Ruby, ItemID.Diamond };
        int gem = gems[Math.Min(5, (tier - 1) * 2 + r.Next(2))];
        var loot = new List<(int, int)>
        {
            (ModContent.ItemType<HadalLens>(), tier),
            (ItemID.GoldCoin, 1 + tier + r.Next(2)),
            (gem, 3 + tier + r.Next(3)),
            (bar, 3 + tier * 2),
            (ItemID.GillsPotion, 2 + r.Next(2)),
            tier switch
            {
                1 => (ItemID.Glowstick, 12 + r.Next(8)),
                2 => (ItemID.ShinePotion, 2),
                3 => (ItemID.SpelunkerPotion, 2),
                _ => (ItemID.HeartreachPotion, 2),
            },
            (ModContent.ItemType<PelagicScale>(), 1 + tier),
        };
        if (tier == 1 && scale > 0) loot.Add((scale, 3 + r.Next(3)));
        if (tier >= 3) loot.Add((ItemID.EndurancePotion, 1 + tier - 3));
        if (tier == 4) loot.Add((ItemID.LifeCrystal, 1));
        return loot.ToArray();
    }

    // Breathing stop dug into the rock behind the cavern's far tip: water
    // passage, a two-cell bubble membrane (players pass, liquid does not) and a
    // dry 12x6 room with a torch. The whole frame must be solid abyss stone.
    private static bool AirPocket(Context c, int cx, int y, int d, int rx)
    {
        int tip = cx + d * rx;
        int E(int k) => tip + d * k;
        if (Math.Min(E(-3), E(17)) < EdgeFrom || Math.Max(E(-3), E(17)) >= EdgeMax) return false;
        for (int k = 3; k <= 17; k++)
        for (int yy = y - 5; yy <= y + 2; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            if (!Solid(t) || !c.Carvable[t.TileType]) return false;
        }
        for (int k = -3; k <= 2; k++)
        for (int yy = y - 2; yy <= y + 1; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            if (t.HasTile ? !c.Carvable[t.TileType] : t.LiquidAmount > 0 && t.LiquidType != LiquidID.Water) return false;
            if (InRoute(c, E(k), yy)) return false;
        }
        Rectangle frame = new(Math.Min(E(-3), E(17)), y - 5, 21, 8);
        Rectangle padded = frame; padded.Inflate(4, 4);
        foreach (Rectangle p in c.Placed)
            if (p.Intersects(padded) && !(p.Contains(E(-3), y) && !p.Contains(E(3), y))) return false;
        for (int k = 3; k <= 17; k++)
        for (int yy = y - 5; yy <= y + 2; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            t.Slope = SlopeType.Solid; t.IsHalfBlock = false; t.LiquidAmount = 0;
        }
        for (int k = -3; k <= 2; k++)
        for (int yy = y - 2; yy <= y + 1; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            if (t.HasTile) { t.ClearTile(); c.R.Cells++; }
            t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
        }
        for (int k = 3; k <= 4; k++)
        for (int yy = y - 2; yy <= y + 1; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            t.ClearTile(); t.HasTile = true; t.TileType = TileID.Bubble; t.LiquidAmount = 0;
        }
        for (int k = 5; k <= 16; k++)
        for (int yy = y - 4; yy <= y + 1; yy++)
        {
            Tile t = Cell(c, E(k), yy);
            t.ClearTile(); t.LiquidAmount = 0;
            c.R.Cells++;
        }
        c.Placed.Add(frame);
        Rectangle world = World(c, -3 * d + tip, E(17), y - 6, y + 3);
        WorldGen.RangeFrame(world.Left, world.Top, world.Right, world.Bottom);
        WorldGen.PlaceTile(X(c, E(11)), y + 1, TileID.Torches, mute: true, forced: true);
        c.R.AirPockets++;
        return true;
    }

    // Gem/ore lens in the cavern's far wall, partly exposed to the cavern.
    private static void Vein(Context c, int cx, int y, int d, int rx, int ry, int tier)
    {
        ushort[] gems = { TileID.Amethyst, TileID.Topaz, TileID.Sapphire, TileID.Emerald, TileID.Ruby, TileID.Diamond };
        int silver = WorldGen.SavedOreTiers.Silver > 0 ? WorldGen.SavedOreTiers.Silver : TileID.Silver;
        int gold = WorldGen.SavedOreTiers.Gold > 0 ? WorldGen.SavedOreTiers.Gold : TileID.Gold;
        ushort type = c.Random.Next(2) == 0 ? gems[Math.Min(5, (tier - 1) * 2 + c.Random.Next(2))] : (ushort)(tier == 1 ? silver : gold);
        double angle = (c.Random.NextDouble() - 0.5) * 2.2;
        int ve = cx + d * (int)(Math.Cos(angle) * rx * 1.15), vy = y + (int)(Math.Sin(angle) * ry * 1.25);
        int radius = 3 + c.Random.Next(3), placed = 0;
        for (int e = ve - radius; e <= ve + radius; e++)
        for (int yy = vy - radius; yy <= vy + radius; yy++)
        {
            if ((e - ve) * (e - ve) + (yy - vy) * (yy - vy) > radius * radius) continue;
            if (e < EdgeFrom || e >= EdgeMax || yy < c.Top || yy > c.RouteBottom) continue;
            Tile t = Cell(c, e, yy);
            if (!Solid(t) || !c.Carvable[t.TileType]) continue;
            t.TileType = type; t.Slope = SlopeType.Solid; t.IsHalfBlock = false;
            placed++;
        }
        if (placed == 0) return;
        Rectangle area = World(c, ve - radius, ve + radius, vy - radius, vy + radius);
        WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
        c.R.Veins++; c.R.VeinCells += placed;
    }
}
