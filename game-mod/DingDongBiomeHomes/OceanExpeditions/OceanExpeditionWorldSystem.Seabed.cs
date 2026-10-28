using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static DingDongBiomeHomes.OceanExpeditions.OceanContentKit;

namespace DingDongBiomeHomes.OceanExpeditions;

// Roadmap item 7: the open sea must not read as an empty tank. Shipwrecks and
// sunken colonnades with caches sit on the deep floor, coral gardens and kelp
// beds dress the floor between them. Everything is placed on the carved basin
// floor only (owned water above, owned solid below), opens the floor only for a
// water-filled chest niche, never leaves a dry cell and stays out of the SOTS Amber sandbank band.
public sealed partial class OceanExpeditionWorldSystem
{
    private const int SeabedEdgeFrom = AmberBankEdgeTo + 20;
    private const int SeabedSurfaceClearance = 24;

    private static string DecorateSeabed(OceanExpeditionLayout p, Admission admission)
    {
        int width = p.Width;
        int wreckTarget = Math.Clamp(width / 900, 2, 6), ruinTarget = Math.Clamp(width / 1400, 1, 4);
        int gardenTarget = Math.Clamp(width / 300, 4, 16), kelpTarget = Math.Clamp(width / 450, 3, 12);
        int wrecks = 0, ruins = 0, gardens = 0, kelp = 0, chests = 0, plants = 0, rejected = 0;
        var taken = new List<Rectangle>();
        var random = new Random(p.Seed ^ 0x5EABED07);
        bool isolated = IsolatedGenRand.Run(p.Seed ^ 0x2E5EA8ED, () =>
        {
            int deepEnd = (int)(width * .62);
            for (int i = 0; i < wreckTarget; i++)
            {
                int hw = 22 + random.Next(9);
                int target = Spread(SeabedEdgeFrom, deepEnd, i, wreckTarget, random);
                if (!FindSeabedSite(p, admission, taken, target, hw, 22, out Rectangle area, out int centerEdge, out int baseFloor)) { rejected++; continue; }
                if (BuildWreck(p, area, centerEdge, hw, baseFloor, random)) chests++;
                Finish(area); taken.Add(area); wrecks++;
            }
            for (int i = 0; i < ruinTarget; i++)
            {
                int hw = 16 + random.Next(5);
                int target = Spread((int)(width * .20), deepEnd, i, ruinTarget, random);
                if (!FindSeabedSite(p, admission, taken, target, hw, 20, out Rectangle area, out int centerEdge, out int baseFloor)) { rejected++; continue; }
                if (BuildRuin(p, area, centerEdge, hw, baseFloor, random)) chests++;
                Finish(area); taken.Add(area); ruins++;
            }
            for (int i = 0; i < gardenTarget; i++)
            {
                int hw = 12 + random.Next(11);
                int target = Spread(SeabedEdgeFrom, width - 60, i, gardenTarget, random);
                if (!FindSeabedSite(p, admission, taken, target, hw, 12, out Rectangle area, out int centerEdge, out _)) { rejected++; continue; }
                plants += BuildCoralGarden(p, centerEdge, hw, random);
                Finish(area); taken.Add(area); gardens++;
            }
            for (int i = 0; i < kelpTarget; i++)
            {
                int hw = 18 + random.Next(15);
                int target = Spread(SeabedEdgeFrom, width - 60, i, kelpTarget, random);
                if (!FindSeabedSite(p, admission, taken, target, hw, 28, out Rectangle area, out int centerEdge, out _)) { rejected++; continue; }
                plants += BuildKelpBed(p, centerEdge, hw, random);
                Finish(area); taken.Add(area); kelp++;
            }
        });
        return $"wrecks={wrecks}/{wreckTarget}|ruins={ruins}/{ruinTarget}|coralGardens={gardens}/{gardenTarget}|kelpBeds={kelp}/{kelpTarget}|lootChests={chests}|plants={plants}|rejectedSites={rejected}|genRandIsolated={isolated}";
    }

    private static int Spread(int from, int to, int index, int count, Random random)
    {
        int pitch = Math.Max(1, (to - from) / Math.Max(1, count));
        return from + pitch * index + pitch / 2 + random.Next(-pitch / 4, pitch / 4 + 1);
    }

    // Decorations are not registered as protected structures: no later pass
    // writes the open-sea basin, and registering them would grow the capture of
    // ForeignStructureIntegrityGuard at the residence-pipeline memory peak.
    private static void Finish(Rectangle area)
    {
        WorldGen.RangeFrame(area.Left, area.Top, area.Right, area.Bottom);
        Flood(area);
    }

    // Seaward/landward shifts of the requested centre; the first admissible
    // site wins, so the result depends only on the layout and the seed.
    private static bool FindSeabedSite(OceanExpeditionLayout p, Admission admission, List<Rectangle> taken,
        int target, int halfWidth, int height, out Rectangle area, out int centerEdge, out int baseFloor)
    {
        foreach (int shift in new[] { 0, 40, -40, 80, -80, 120, -120 })
        {
            centerEdge = target + shift;
            if (SeabedSiteFree(p, admission, taken, centerEdge, halfWidth, height, out area, out baseFloor)) return true;
        }
        area = default; centerEdge = 0; baseFloor = 0;
        return false;
    }

    private static bool SeabedSiteFree(OceanExpeditionLayout p, Admission admission, List<Rectangle> taken,
        int centerEdge, int halfWidth, int height, out Rectangle area, out int baseFloor)
    {
        area = default; baseFloor = 0;
        int from = centerEdge - halfWidth, to = centerEdge + halfWidth;
        if (from < SeabedEdgeFrom || to >= p.Width - 24) return false;
        int low = int.MaxValue, high = int.MinValue;
        for (int edge = from; edge <= to; edge++)
        {
            int f = p.Floor(edge);
            low = Math.Min(low, f); high = Math.Max(high, f);
        }
        // A flat enough bed: the content never bridges a slope with open water below.
        if (high - low > 4) return false;
        baseFloor = p.Floor(centerEdge);
        area = new Rectangle(Math.Min(p.X(from), p.X(to)), low - height, to - from + 1, high - low + height + 4);
        if (area.Top < p.SeaLevel + SeabedSurfaceClearance) return false;
        Rectangle envelope = area; envelope.Inflate(4, 4);
        if (!p.Bounds.Contains(envelope) || !admission.Allows(envelope)) return false;
        foreach (var room in p.Grottos)
        {
            Rectangle padded = room.Bounds; padded.Inflate(24, 24);
            if (padded.Intersects(area)) return false;
        }
        foreach (var mark in p.Landmarks)
            if (new Rectangle(mark.Floor.X - 56, mark.Floor.Y - 44, 113, 94).Intersects(area)) return false;
        foreach (var other in taken)
        {
            Rectangle padded = other; padded.Inflate(16, 16);
            if (padded.Intersects(area)) return false;
        }
        for (int x = area.Left; x < area.Right; x++)
        {
            int f = p.Floor(p.Edge(x));
            for (int y = area.Top; y < area.Bottom; y++)
            {
                Tile t = Main.tile[x, y];
                if (y < f)
                {
                    // Owned open water; the basin's own scattered coral may be replaced.
                    if (t.HasTile && t.TileType != TileID.Coral && t.TileType != TileID.Seaweed) return false;
                    if (t.LiquidAmount < 200 || t.LiquidType != LiquidID.Water) return false;
                }
                else if (!t.HasTile || !Main.tileSolid[t.TileType]) return false;
            }
        }
        return NoChestNear(p.X(centerEdge), baseFloor, 30);
    }

    // A broken hull half sunk into the sand: keel, curved bottom, end walls with
    // a breach, a deck with a collapsed middle, a snapped mast and a barrel cache.
    private static bool BuildWreck(OceanExpeditionLayout p, Rectangle area, int centerEdge, int hw, int baseFloor, Random random)
    {
        int cx = p.X(centerEdge), keel = baseFloor + 1, deck = keel - 10;
        int breach = random.Next(2) == 0 ? -1 : 1, rot = random.Next(11);
        int Hull(int dx) => keel - (int)Math.Round(5 * (dx / (double)hw) * (dx / (double)hw));
        for (int dx = -hw; dx <= hw; dx++)
        {
            int x = cx + dx, f = p.Floor(p.Edge(x)), hb = Hull(dx);
            Solid(x, hb, TileID.WoodBlock); Solid(x, hb + 1, TileID.WoodBlock);
            for (int y = hb + 2; y < f; y++) Solid(x, y, TileID.Sand);
            bool end = Math.Abs(dx) >= hw - 1;
            if (end)
            {
                int top = dx > 0 ? deck - 3 : deck;
                for (int y = top; y < hb; y++)
                    if (!(Math.Sign(dx) == breach && y >= deck + 2 && y <= deck + 5)) Solid(x, y, TileID.WoodBlock);
                continue;
            }
            for (int y = deck + 1; y < hb; y++)
                if ((dx * 7 + y * 3 + rot) % 9 != 0) Main.tile[x, y].WallType = WallID.Wood;
            bool collapsed = dx >= -3 && dx <= 5 || (dx * 7 + rot) % 11 == 0;
            if (!collapsed) Solid(x, deck, TileID.WoodBlock);
        }
        int mast = cx - hw / 3, mastHeight = 10 + random.Next(9);
        for (int y = deck - 1; y >= deck - mastHeight && y > area.Top; y--)
        {
            Tile t = Main.tile[mast, y]; t.ClearTile(); t.HasTile = true; t.TileType = TileID.WoodenBeam;
        }
        int[] treasure = { ItemID.BreathingReed, ItemID.Flipper, ItemID.FloatingTube, ItemID.WaterWalkingBoots, ItemID.Trident, ItemID.DivingHelmet };
        int chestX = cx + hw / 3, chestFloor = Math.Min(Hull(chestX - cx), Hull(chestX + 1 - cx));
        if (!NoChestNear(chestX, chestFloor, 24)) return false;
        return LootChest(chestX, chestFloor, TileID.WoodBlock, 5, new (int, int)[]
        {
            (treasure[random.Next(treasure.Length)], 1), (ItemID.GoldCoin, 2 + random.Next(3)),
            (ModContent.ItemType<PelagicScale>(), 2 + random.Next(3)), (ItemID.GillsPotion, 2),
            (ItemID.SonarPotion, 2), (ItemID.CratePotion, 1), (ItemID.Seashell, 3 + random.Next(4))
        }) >= 0;
    }

    // A marble floor with broken columns and a lintel, a water chest in the middle.
    private static bool BuildRuin(OceanExpeditionLayout p, Rectangle area, int centerEdge, int hw, int baseFloor, Random random)
    {
        int cx = p.X(centerEdge), slab = baseFloor - 1;
        for (int dx = -hw; dx <= hw; dx++)
        {
            int x = cx + dx, f = p.Floor(p.Edge(x));
            Solid(x, slab, TileID.MarbleBlock);
            for (int y = slab + 1; y < f; y++) Solid(x, y, TileID.Sand);
        }
        int[] columns = { -hw + 2, -hw / 3, hw / 3, hw - 2 };
        int[] heights = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            heights[i] = 6 + random.Next(11);
            for (int dx = -1; dx <= 1; dx++)
            for (int y = slab - 1; y >= slab - heights[i] && y > area.Top; y--)
                Solid(cx + columns[i] + dx, y, TileID.MarbleBlock);
        }
        // One lintel joins the tallest neighbouring pair when both still stand.
        int pair = heights[0] + heights[1] >= heights[2] + heights[3] ? 0 : 2;
        int lintel = slab - Math.Min(heights[pair], heights[pair + 1]);
        if (Math.Min(heights[pair], heights[pair + 1]) >= 9)
            for (int x = cx + columns[pair] - 1; x <= cx + columns[pair + 1] + 1; x++) Solid(x, lintel, TileID.MarbleBlock);
        for (int dx = -hw + 4; dx <= hw - 4; dx += 5)
            if (Math.Abs(dx) > 3) Solid(cx + dx, slab - 1, TileID.Coralstone);
        int[] treasure = { ItemID.MagicConch, ItemID.Flipper, ItemID.BreathingReed, ItemID.WaterWalkingBoots };
        if (!NoChestNear(cx, slab, 24)) return false;
        return LootChest(cx, slab, TileID.MarbleBlock, 17, new (int, int)[]
        {
            (treasure[random.Next(treasure.Length)], 1), (ItemID.GoldCoin, 3 + random.Next(3)),
            (ModContent.ItemType<HadalLens>(), 2 + random.Next(2)), (ModContent.ItemType<GrottoGeode>(), 2 + random.Next(3)),
            (ItemID.SpelunkerPotion, 2), (ItemID.ShinePotion, 2), (ItemID.FlipperPotion, 2)
        }) >= 0;
    }

    // Low sand mounds carrying coral, seaweed tufts and a few coralstone boulders.
    private static int BuildCoralGarden(OceanExpeditionLayout p, int centerEdge, int hw, Random random)
    {
        int cx = p.X(centerEdge), placed = 0;
        int[] tops = new int[hw * 2 + 1];
        for (int dx = -hw; dx <= hw; dx++)
        {
            int x = cx + dx, f = p.Floor(p.Edge(x));
            double t = dx / (double)hw;
            int mound = (int)(3 * (1 - t * t)) + (random.Next(3) == 0 ? 1 : 0);
            for (int y = f - mound; y < f; y++) Solid(x, y, TileID.Sand);
            tops[dx + hw] = f - mound;
        }
        for (int dx = -hw; dx <= hw; dx++)
        {
            int x = cx + dx, top = tops[dx + hw];
            switch ((dx + hw) % 3)
            {
                case 0:
                    placed += Coral(x, top) ? 1 : 0;
                    break;
                case 1:
                    placed += Seaweed(x, top, 3 + random.Next(6)) > 0 ? 1 : 0;
                    break;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            int dx = random.Next(-hw + 2, hw - 2), top = Math.Min(tops[dx + hw], tops[dx + hw + 1]);
            if (Main.tile[cx + dx, top - 1].HasTile || Main.tile[cx + dx + 1, top - 1].HasTile) continue;
            for (int y = top - 2; y < top; y++) { Solid(cx + dx, y, TileID.Coralstone); Solid(cx + dx + 1, y, TileID.Coralstone); }
        }
        return placed;
    }

    // Dense seaweed chains of uneven height swaying above the sand.
    private static int BuildKelpBed(OceanExpeditionLayout p, int centerEdge, int hw, Random random)
    {
        int cx = p.X(centerEdge), placed = 0;
        for (int dx = -hw; dx <= hw; dx += 1 + random.Next(2))
        {
            int x = cx + dx, f = p.Floor(p.Edge(x));
            double t = dx / (double)hw;
            int height = (int)((8 + random.Next(14)) * (1 - .5 * t * t));
            placed += Seaweed(x, f, Math.Max(3, height)) > 0 || Coral(x, f) ? 1 : 0;
        }
        return placed;
    }

    // Fallback dressing when native seaweed rules refuse a column.
    private static bool Coral(int x, int sandRow)
    {
        if (Main.tile[x, sandRow - 1].HasTile) return false;
        WorldGen.PlaceTile(x, sandRow - 1, TileID.Coral, mute: true);
        return Main.tile[x, sandRow - 1].HasTile;
    }
}
