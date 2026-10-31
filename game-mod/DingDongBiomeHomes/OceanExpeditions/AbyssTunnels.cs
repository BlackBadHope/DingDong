using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.OceanExpeditions;

// S34 (owner, 31.10: "combine everything"): the Abyss band gets a cave system in the spirit of
// Infernum Mode's custom Abyss (InfernumTeam/InfernumMode, MIT; ideas only, no code copied):
// noise "spaghetti" tunnels through the upper third with kelp forests on their floors, and in the
// lower third worm tunnels that wind towards a central pit, crystal grottos (Lumenyl, deep layers
// only) and thermal vent fields. Like AbyssCaverns it only clears Calamity's own abyss rock, keeps
// every wall, stays out of Ding Dong's shafts, crossings and stations and out of protected
// structures, and floods what it opens. Runs inside FinishAbyss's IsolatedGenRand scope.
internal static class AbyssTunnels
{
    internal sealed class Receipt
    {
        public int Spaghetti, Chambers, Worms, WormCells, Crystals, Kelp, Flora, Vents, Protected;
        public override string ToString() =>
            $"tunnelCells={Spaghetti}|chamberCells={Chambers}|worms={Worms}|wormCells={WormCells}|crystals={Crystals}|kelp={Kelp}|tunnelFlora={Flora}|vents={Vents}|protectedBlocks={Protected}";
    }

    private const int EdgeFrom = AbyssRouteGeometry.EdgeFrom + 30, EdgeTo = 880, RoutePad = 10, Block = 16;

    private static ushort Find(string name) => ModContent.TryFind<ModTile>("CalamityMod/" + name, out var t) ? t.Type : (ushort)0;
    private static ushort[] Types(params string[] names)
    {
        var list = new List<ushort>();
        foreach (string n in names) { ushort t = Find(n); if (t != 0) list.Add(t); }
        return list.ToArray();
    }

    private static uint Hash(int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177; return h ^ (h >> 16);
        }
    }
    private static double Lattice(int x, int y, int salt) => (Hash(x, y, salt) & 0xFFFFFF) / (double)0xFFFFFF;
    private static double Noise(double x, double y, int salt)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = x - ix, fy = y - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Lattice(ix, iy, salt), b = Lattice(ix + 1, iy, salt), c = Lattice(ix, iy + 1, salt), d = Lattice(ix + 1, iy + 1, salt);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
    // Signed fBm in [-1, 1].
    private static double Fbm(double x, double y, int salt) =>
        (Noise(x, y, salt) * .55 + Noise(x * 2.03, y * 2.03, salt + 1) * .3 + Noise(x * 4.1, y * 4.1, salt + 2) * .15) * 2 - 1;

    internal static void Generate(bool left, int top, int routeBottom, int crossings, int seed, Random random, Receipt r)
    {
        bool[] carvable = new bool[TileLoader.TileCount];
        foreach (ushort t in Types("SulphurousShale", "AbyssGravel", "PyreMantle", "Voidstone", "SulphurousSandstone", "HardenedSulphurousSandstone"))
            carvable[t] = true;
        ushort lumenyl = Find("LumenylCrystals"), vent = Find("ThermalVent");
        if (Find("AbyssGravel") == 0) return;
        ushort[] kelp = Types("AbyssGiantKelp1", "AbyssGiantKelp2", "AbyssGiantKelp3", "AbyssGiantKelp4", "AbyssKelp");
        ushort[] upperFlora = Types("SulphurTubeCoral", "SulphurTentacleCorals", "ShalePile1", "ShalePile2", "GravelPile1", "SpiderCoral1", "SpiderCoral2");
        ushort[] lowerFlora = Types("GravelPile2", "GravelPile3", "SpiderCoral3", "SpiderCoral4", "BulbTree1", "BulbTree2", "PhoviamareHalm", "PlantyMushPile1");
        int X(int edge) => left ? edge : Main.maxTilesX - edge;
        int height = routeBottom - top;
        if (height < 600) return;
        int layer1End = top + height / 3, layer3Start = top + height * 2 / 3;

        // Protected structures, sampled once per 16x16 block of the band.
        int bw = (EdgeTo - EdgeFrom) / Block + 2, bh = height / Block + 2;
        var blocked = new bool[bw * bh];
        for (int by = 0; by < bh; by++)
        for (int bx = 0; bx < bw; bx++)
        {
            int e0 = EdgeFrom + bx * Block, y0 = top + by * Block;
            var rect = new Rectangle(Math.Min(X(e0), X(e0 + Block - 1)), y0, Block, Block);
            if (!GenVars.structures.CanPlace(rect, 0)) { blocked[by * bw + bx] = true; r.Protected++; }
        }
        bool Free(int e, int y)
        {
            int bx = (e - EdgeFrom) / Block, by = (y - top) / Block;
            if (bx < 0 || by < 0 || bx >= bw || by >= bh || blocked[by * bw + bx]) return false;
            // Shafts, crossings and stations: keep a pad around the whole route.
            return !AbyssRouteGeometry.InRoute(e, y, top, routeBottom, crossings) && !AbyssRouteGeometry.InRoute(e + RoutePad, y, top, routeBottom, crossings) &&
                !AbyssRouteGeometry.InRoute(e - RoutePad, y, top, routeBottom, crossings) && !AbyssRouteGeometry.InRoute(e, y + RoutePad, top, routeBottom, crossings) &&
                !AbyssRouteGeometry.InRoute(e, y - RoutePad, top, routeBottom, crossings);
        }
        bool Carve(int e, int y)
        {
            if (e < EdgeFrom || e >= EdgeTo || y <= top + 20 || y >= routeBottom - 30) return false;
            Tile t = Main.tile[X(e), y];
            if (!t.HasTile || !carvable[t.TileType] || !Free(e, y)) return false;
            t.HasTile = false; t.Slope = SlopeType.Solid; t.IsHalfBlock = false;
            t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
            return true;
        }

        // Upper two thirds: ridged-noise tunnels |n| < threshold (denser above, sparser in the middle
        // layer), thinning towards the band's edges. S34 x10: the middle layer (~2000 rows of pyre
        // mantle) read as one solid block around the shafts, so it also gets wide noise chambers.
        int salt = seed & 0xFFFF;
        for (int y = top + 30; y < layer3Start; y++)
        {
            double fadeY = Math.Min(1, (y - top - 30) / 60.0) * Math.Min(1, (layer3Start - y) / 60.0);
            double threshold = y < layer1End ? .055 : .07;
            for (int e = EdgeFrom; e < EdgeTo; e++)
            {
                double fadeX = Math.Min(1, Math.Min(e - EdgeFrom, EdgeTo - e) / 50.0);
                double n = Fbm(e / 85.0, y / 55.0, salt);
                bool tunnel = Math.Abs(n) < threshold * fadeX * fadeY;
                bool chamber = y >= layer1End && Fbm(e / 170.0, y / 120.0, salt + 201) * fadeX * fadeY > .30;
                if ((tunnel || chamber) && Carve(e, y)) { if (chamber) r.Chambers++; else r.Spaghetti++; }
            }
        }

        // Lower third: worms from the band's sides winding down to a central pit.
        int pitEdge = (EdgeFrom + EdgeTo) / 2 + random.Next(-80, 81), pitY = routeBottom - 120;
        int worms = 12 + random.Next(4);
        for (int w = 0; w < worms; w++)
        {
            double e = EdgeFrom + 40 + random.NextDouble() * (EdgeTo - EdgeFrom - 80), y = layer3Start + random.NextDouble() * (height / 6.0);
            double heading = random.NextDouble() * Math.PI * 2;
            int steps = 0;
            for (; steps < 900; steps++)
            {
                double toPit = Math.Atan2(pitY - y, pitEdge - e);
                double wander = Fbm(e / 60.0, y / 60.0, salt + 31 + w * 7) * 1.4;
                double pull = Math.Clamp((y - layer3Start) / (double)(pitY - layer3Start), 0, 1) * .25 + .06;
                heading += Math.Atan2(Math.Sin(toPit - heading), Math.Cos(toPit - heading)) * pull + wander * .12;
                e += Math.Cos(heading) * 2.2; y += Math.Sin(heading) * 2.2;
                if (e < EdgeFrom + 10 || e > EdgeTo - 10 || y > pitY + 10) break;
                int radius = 9 + (int)(6 * (Fbm(e / 40.0, y / 40.0, salt + 77) + 1) / 2) + (int)(6 * Math.Clamp((y - layer3Start) / (double)(pitY - layer3Start), 0, 1));
                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                    if (dx * dx + dy * dy <= radius * radius && Carve((int)e + dx, (int)y + dy)) r.WormCells++;
                if (Math.Abs(e - pitEdge) < 30 && Math.Abs(y - pitY) < 30) break;
            }
            if (steps > 40) r.Worms++;
        }
        // The pit itself.
        for (int dy = -45; dy <= 45; dy++)
        for (int dx = -70; dx <= 70; dx++)
            if (dx * dx / 4900.0 + dy * dy / 2025.0 <= 1 + .15 * Fbm((pitEdge + dx) / 20.0, (pitY + dy) / 20.0, salt + 5) && Carve(pitEdge + dx, pitY + dy)) r.WormCells++;

        // Dressing: kelp and coral on the floors of the new tunnels, crystal grottos and vent fields deep.
        for (int y = top + 40; y < routeBottom - 40; y += 2)
        for (int e = EdgeFrom; e < EdgeTo; e += 3)
        {
            int x = X(e);
            Tile here = Main.tile[x, y], below = Main.tile[x, y + 1];
            if (here.HasTile || here.LiquidAmount < 200 || !below.HasTile || !carvable[below.TileType] || !Free(e, y)) continue;
            bool lower = y >= layer3Start;
            int roll = random.Next(100);
            if (!lower && roll < (y < layer1End ? 14 : 8) && kelp.Length > 0)
            {
                WorldGen.PlaceTile(x, y, kelp[random.Next(kelp.Length)], mute: true);
                if (Main.tile[x, y].HasTile) r.Kelp++;
            }
            else if (roll < 24)
            {
                ushort[] set = lower ? lowerFlora : upperFlora;
                if (set.Length > 0) { WorldGen.PlaceTile(x, y, set[random.Next(set.Length)], mute: true); if (Main.tile[x, y].HasTile) r.Flora++; }
            }
            else if (lower && vent != 0 && roll < 27 && Fbm(e / 150.0, y / 150.0, salt + 91) > .35)
            {
                WorldGen.PlaceTile(x, y, vent, mute: true);
                if (Main.tile[x, y].HasTile) r.Vents++;
            }
        }
        // Lumenyl only in the two deep layers (post-Calamitas material, see AbyssCaverns).
        if (lumenyl != 0)
            for (int y = layer3Start; y < routeBottom - 40; y += 2)
            for (int e = EdgeFrom; e < EdgeTo; e += 2)
            {
                if (Fbm(e / 110.0, y / 110.0, salt + 13) < .42 || random.Next(6) != 0) continue;
                int x = X(e);
                Tile t = Main.tile[x, y];
                if (t.HasTile || t.LiquidAmount < 200 || !Free(e, y)) continue;
                bool wall = Main.tile[x + 1, y].HasTile || Main.tile[x - 1, y].HasTile || Main.tile[x, y + 1].HasTile || Main.tile[x, y - 1].HasTile;
                if (!wall) continue;
                WorldGen.PlaceTile(x, y, lumenyl, mute: true);
                if (Main.tile[x, y].HasTile) r.Crystals++;
            }
        int x0 = Math.Min(X(EdgeFrom), X(EdgeTo)), x1 = Math.Max(X(EdgeFrom), X(EdgeTo));
        WorldGen.RangeFrame(x0, top, x1, routeBottom);
    }
}
