using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.OceanExpeditions;

public sealed partial class OceanExpeditionWorldSystem
{
    // Provider reservations retain a broad shell. Individual objects and foreign
    // materials retain a smaller support shell. The maps are immutable per pass.
    private sealed class Admission
    {
        private readonly HashSet<Point> blocked = new();
        private readonly HashSet<Point> detail = new();
        // Layout 3: provider structures that stay inside the sea, as tile rectangles.
        internal readonly List<Rectangle> Kept = new();
        internal int DroppedVanillaComponents { get; private set; }
        internal Admission(OceanExpeditionLayout p)
        {
            bool[] terrain = OceanTerrainPolicy.Create();
            bool[] structureOnly = ContinentalPlacementMaskPolicy.CreateStructureOnly();
            bool layout3 = p.IslandLayout >= 3;
            var denied = new Dictionary<Point, Rectangle>();
            for (int edge = 16; edge < p.Width + 32; edge += 16)
            for (int y = p.SeaLevel - 160; y < p.Bottom + 48; y += 16)
            {
                int x = p.Left ? edge : p.WorldWidth - edge - 16;
                Rectangle b = Rectangle.Intersect(new(x, y, 16, 16), new(1, 1, p.WorldWidth - 2, p.WorldHeight - 2));
                bool deny = !GenVars.structures.CanPlace(b, structureOnly, 12);
                if (!deny)
                {
                    // Layout 3 clears loose objects and foreign material in the open
                    // sea: kept with a support shell they hung as fragments in the water.
                    if (layout3) continue;
                    for (int xx = b.Left; xx < b.Right; xx++)
                    for (int yy = b.Top; yy < b.Bottom; yy++)
                    {
                        Tile t = Main.tile[xx, yy];
                        if ((t.HasTile && (t.TileType >= terrain.Length || !terrain[t.TileType])) || t.WallType >= WallID.Count)
                            // Preserve exact objects and foreign material with a
                            // four-tile support shell, not a 64-tile terrain plug.
                            for(int dx=(xx-4)/4;dx<=(xx+4)/4;dx++)
                            for(int dy=(yy-4)/4;dy<=(yy+4)/4;dy++)detail.Add(new(dx,dy));
                    }
                    continue;
                }
                if (layout3) { denied[new(edge / 16, (y - (p.SeaLevel - 160)) / 16)] = b; continue; }
                Block(b);
            }
            if (!layout3) return;
            // Layout 3: protected areas made only of vanilla material (cabins, caves,
            // the replaced vanilla sea) are carved; a provider's own structure (any
            // modded tile or wall) or shimmer stays and is grown into a seamount.
            var seen = new HashSet<Point>();
            foreach (Point start in denied.Keys)
            {
                if (!seen.Add(start)) continue;
                var component = new List<Rectangle>();
                var queue = new Queue<Point>(); queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    Point c = queue.Dequeue(); component.Add(denied[c]);
                    foreach (Point n in new[] { new Point(c.X + 1, c.Y), new Point(c.X - 1, c.Y), new Point(c.X, c.Y + 1), new Point(c.X, c.Y - 1) })
                        if (denied.ContainsKey(n) && seen.Add(n)) queue.Enqueue(n);
                }
                if (!HasProviderContent(component)) { DroppedVanillaComponents++; continue; }
                Rectangle bounds = component[0];
                foreach (Rectangle r in component) { Block(r); bounds = Rectangle.Union(bounds, r); }
                Kept.Add(bounds);
            }
        }
        private void Block(Rectangle b)
        {
            // Align cells to world coordinates, including mirrored coasts.
            for (int xx = (b.Left - 16) / 16; xx <= (b.Right + 16) / 16; xx++)
            for (int yy = (b.Top - 16) / 16; yy <= (b.Bottom + 16) / 16; yy++) blocked.Add(new(xx, yy));
        }
        private static bool HasProviderContent(List<Rectangle> blocks)
        {
            foreach (Rectangle b in blocks)
            for (int x = b.Left; x < b.Right; x++)
            for (int y = b.Top; y < b.Bottom; y++)
            {
                Tile t = Main.tile[x, y];
                if ((t.HasTile && t.TileType >= TileID.Count) || t.WallType >= WallID.Count ||
                    (t.LiquidAmount > 0 && t.LiquidType == LiquidID.Shimmer))
                    return true;
            }
            return false;
        }
        internal bool Allows(int x, int y) => !blocked.Contains(new(x / 16, y / 16))&&!detail.Contains(new(x/4,y/4));
        internal bool Allows(Rectangle area)
        {
            for (int x = area.Left/4; x <= (area.Right-1)/4; x++)
            for (int y = area.Top/4; y <= (area.Bottom-1)/4; y++)
                if (!Allows(Math.Max(area.Left,x*4),Math.Max(area.Top,y*4))) return false;
            return true;
        }
    }
    private const int FloorSealDepth = 160;
    // Admission maps cover edges up to Width+32; the dam must stay inside them.
    private const int DamWidth = 32;
    // SOTS places its Amber vault where at least eight water cells sit over a sand
    // run of at least fourteen rows within 51..400 tiles of the coast. Before the
    // reservation shell that anchor was found in a preserved piece of vanilla sea
    // floor; now the shelf near the shore carries a sandbank deep enough on its own.
    private const int AmberBankEdgeFrom = 40, AmberBankEdgeTo = 420, AmberBankDepth = 40;
    private static int FloorSandDepth(int edge) =>
        edge >= AmberBankEdgeFrom && edge <= AmberBankEdgeTo ? AmberBankDepth : 4;
    internal static void Apply(OceanExpeditionLayout p)
    {
        Admission admission = new(p);
        long carved = 0, retained = 0, caveWater = 0;
        bool caves = p.IslandLayout >= 3;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), floor = p.Floor(edge); var column=p.IslandColumn(edge);
            // Layout 3 owns the whole column down to the Underworld seal: open sea, then cave rock.
            int end = caves ? p.Bottom : floor + 24;
            for (int y = p.SeaLevel - 64; y < end; y++)
            {
                if (!admission.Allows(x, y) || !p.Owns(edge, y)) { retained++; continue; }
                Tile t = Main.tile[x, y];
                t.ClearEverything();
                bool island = y>=column.Top&&y<=column.Bottom;
                if (!island && caves && y >= floor && p.CaveOpen(edge, y, floor))
                {
                    t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; carved++; caveWater++;
                    continue;
                }
                if (island || y >= floor)
                {
                    t.HasTile = true;
                    // Sand over an opening would pour into it the first time a neighbour is framed.
                    t.TileType = island ? IsletMaterial(p, column, y) :
                        y < floor + FloorSandDepth(edge) ? (caves && p.CaveOpen(edge, y + 1, floor) ? TileID.Coralstone : TileID.Sand) :
                        caves ? CaveRock(p, edge, y, floor) : TileID.Stone;
                }
                else if (y >= p.SeaLevel)
                {
                    t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; carved++;
                    // Layout 4: the islet's background continues down to the floor (owner, 31.10).
                    if (p.InGhost(edge, y)) t.WallType = p.GhostWall(edge, y);
                }
            }
        }
        long orphans = 0, embedded = 0; string seamountContent = "none";
        if (p.IslandLayout >= 3)
        {
            orphans = ClearOrphanedObjects(p, admission);
            embedded = EmbedProviderStructures(p, admission);
            long skinned = SkinSeamounts(p, admission);
            seamountContent = (p.IslandLayout >= 4 ? DecorateGhostStems(p, admission) : DecorateSeamounts(p, admission)) + ",skin:" + skinned + "|caves=water:" + caveWater + "," + DecorateCaves(p, admission);
        }
        // The basin floor now sits in the cavern layer. Natural caves right under
        // the floor band drain the sea when the world settles, so plug them with
        // stone down to the first solid run; provider reservations stay untouched.
        long sealedCells = 0;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), start = (caves ? p.Bottom : p.Floor(edge) + 24), solidRun = 0;
            for (int y = start; y < start + FloorSealDepth && y < Main.maxTilesY - 200; y++)
            {
                if (!admission.Allows(x, y)) { solidRun = 0; continue; }
                Tile t = Main.tile[x, y];
                if (t.HasTile && Main.tileSolid[t.TileType]) { if (++solidRun >= 16) break; continue; }
                solidRun = 0;
                t.ClearEverything(); t.HasTile = true; t.TileType = TileID.Stone; sealedCells++;
            }
        }
        // Settling drained the basin to a constant level far below the sea: a
        // side spill into caves at the carve boundary, not a bounded pocket. Plug
        // every open cell in a dam band on both sides: the untouched map-edge
        // columns and the inland columns just past the shelf, from the surface
        // down to the basin floor. Provider reservations stay untouched.
        long damCells = 0;
        // Layout 3: the inland dam only needs to reach the shelf, not the abyssal plain.
        int inlandBottom = p.Bottom + 24;
        if (p.IslandLayout >= 3)
        {
            inlandBottom = p.SeaLevel;
            for (int edge = Math.Max(20, p.Width - 64); edge < p.Width; edge++) inlandBottom = Math.Max(inlandBottom, p.Floor(edge) + 24);
            inlandBottom = Math.Min(p.Bottom + 24, inlandBottom + 16);
        }
        for (int edge = 1; edge < p.Width + DamWidth; edge++)
        {
            if (edge >= 20 && edge < p.Width) continue;
            int x = p.X(edge);
            if (x < 1 || x >= Main.maxTilesX - 1) continue;
            int damBottom = edge >= p.Width ? inlandBottom : p.Bottom + 24;
            for (int y = p.SeaLevel - 8; y < damBottom && y < Main.maxTilesY - 200; y++)
            {
                if (!admission.Allows(x, y)) continue;
                Tile t = Main.tile[x, y];
                if (t.HasTile && Main.tileSolid[t.TileType]) continue;
                t.ClearEverything(); t.HasTile = true; t.TileType = TileID.Stone; damCells++;
            }
        }
        // RC14 showed the dam alone changes nothing: the water never reached the
        // inland columns, the map edge or the caves under the floor, yet the sea
        // settled to the same level every cycle. The only openings the dam and the
        // floor seal leave are provider reservations, whose own passages drain the
        // basin into the cavern network. Liquid moves four-directionally, so one
        // owned stone cell against every reserved cell makes the basin watertight
        // without touching a single reserved tile.
        long shellCells = 0;
        for (int edge = 1; edge < p.Width + DamWidth; edge++)
        {
            int x = p.X(edge);
            if (x < 2 || x >= Main.maxTilesX - 2) continue;
            int bottom = edge >= 20 && edge < p.Width ? (caves ? p.Bottom + 24 : p.Floor(edge) + 24) : edge >= p.Width ? inlandBottom : p.Bottom + 24;
            for (int y = p.SeaLevel - 8; y < bottom && y < Main.maxTilesY - 200; y++)
            {
                if (!admission.Allows(x, y)) continue;
                Tile t = Main.tile[x, y];
                if (t.HasTile && Main.tileSolid[t.TileType]) continue;
                if (admission.Allows(x - 1, y) && admission.Allows(x + 1, y) && admission.Allows(x, y - 1) && admission.Allows(x, y + 1)) continue;
                t.ClearEverything(); t.HasTile = true; t.TileType = TileID.Stone; shellCells++;
            }
        }
        var rooms = p.Grottos.ToArray(); p.Grottos.Clear();
        int grottoCandidates=0,blockedGrottos=0;
        var deferredRooms = new List<OceanGrotto>();
        int roomIndex = -1;
        foreach (var room in rooms)
        {
            roomIndex++;
            // Small bounded alternatives keep the requested content when one
            // native provider has reserved the first candidate site.
            bool placed=false;
            foreach(float size in new[]{1f,.75f,.50f})
            {
            foreach(int dy in new[]{0,-96,96,-192,192,-288,288,-384,384})
            {
              foreach(int shift in new[]{0,-24,24,-48,48})
              {
                int cy=room.Bounds.Center.Y+dy;
                if(cy<p.SeaLevel+p.Depth*.12||cy>p.Bottom-140)continue;
                int w=Math.Max(101,(int)(room.Bounds.Width*size))|1,h=Math.Max(49,(int)(room.Bounds.Height*size))|1;
                // Layout 3 has no cliff above the cave mass: the room sits beside a shaft, its
                // airlock mouth (toward lower edges) opening into the shaft, its shell clear of it.
                int edge=p.IslandLayout>=3 ? (int)p.ShaftCentre(roomIndex % p.ShaftCount, cy)+w/2+10+Math.Abs(shift) : p.CliffEdge(cy)+w/6+shift;
                Rectangle b = new(p.X(edge)-w/2,cy-h/2,w,h);
                Rectangle envelope = b; envelope.Inflate(16, 16);
                grottoCandidates++;
                if (!p.Bounds.Contains(envelope) || !admission.Allows(envelope)) { blockedGrottos++;continue; }
                if(p.Floor(p.Edge(b.Center.X)+b.Width/4)>b.Center.Y)continue;
                if (p.Grottos.Any(g => { Rectangle padded = g.Bounds; padded.Inflate(32, 32); return padded.Intersects(b); })) continue;
                CarveGrotto(p, b); p.Grottos.Add(new(b)); GenVars.structures.AddProtectedStructure(envelope, 6); placed=true;break;
              }
              if(placed)break;
            }
            if(placed)break;
            }
            if (!placed) deferredRooms.Add(room);
        }
        // A provider can occupy the entire narrow cliff strip. Keep its
        // reservations intact and search the surrounding sea for sealed reefs.
        int adaptiveGrottos = 0;
        foreach (var room in deferredRooms)
            if (TryPlaceAdaptiveGrotto(p, admission, room, rooms.Length, ref grottoCandidates, ref blockedGrottos))
                adaptiveGrottos++;
        var landmarks = p.Landmarks.ToArray(); p.Landmarks.Clear();
        foreach (var mark in landmarks)
        {
            Rectangle b = new(mark.Floor.X - 32, mark.Floor.Y - 20, 65, 46);
            if (!p.Bounds.Contains(b) || !admission.Allows(b) || p.Grottos.Any(g => g.Bounds.Intersects(b))) continue;
            PlaceLandmark(mark, p.SeaLevel); p.Landmarks.Add(mark); GenVars.structures.AddProtectedStructure(b, 4);
        }
        // Frame only the added local features; a 25-million-tile RangeFrame is
        // unnecessary for full solid/empty interior cells and wastes minutes.
        foreach (var room in p.Grottos)
            WorldGen.RangeFrame(room.Bounds.Left, room.Bounds.Top, room.Bounds.Right, room.Bounds.Bottom);
        foreach (var island in p.Islands)
        {
            foreach (int direction in new[] {-1,1})
            {
                int edge = island.Edge + direction * island.Radius * 2 / 3;
                int x = p.X(edge), y = p.IslandFloor(edge);
                if (y > 80 && y < p.SeaLevel && admission.Allows(new Rectangle(x-18,y-70,37,74)))
                    WorldGen.GrowPalmTree(x,y);
            }
            // Harbour islets stay open for the shore residences; the others get a
            // wooded crown so they read as land rather than a bare sand bar.
            if (island.Harbor || p.IslandLayout < 2) continue;
            if (p.IslandLayout >= 3)
            {
                // Owner, 29.10: the islets read empty. A castaway's cache on the crown and palms on both beaches.
                int cx = p.X(island.Edge + island.Radius / 5), cy = p.IslandFloor(island.Edge + island.Radius / 5);
                if (cy > 80 && cy < p.SeaLevel && admission.Allows(new Rectangle(cx - 4, cy - 6, 9, 8)) && OceanContentKit.NoChestNear(cx, cy, 30))
                    PlaceCache(cx, cy, 0);
                foreach (double offset in new[] { -.85, .85 })
                {
                    int edge = island.Edge + (int)(offset * island.Radius);
                    int x = p.X(edge), y = p.IslandFloor(edge);
                    if (y > 80 && y < p.SeaLevel && admission.Allows(new Rectangle(x - 18, y - 70, 37, 74)))
                        WorldGen.GrowPalmTree(x, y);
                }
            }
            foreach (double offset in new[] { -.42, -.2, .05, .3 })
            {
                int edge = island.Edge + (int)(offset * island.Radius);
                int x = p.X(edge), y = p.IslandFloor(edge);
                if (y > 80 && y < p.SeaLevel - 4 && Main.tile[x, y].TileType == TileID.Grass &&
                    admission.Allows(new Rectangle(x-12,y-42,25,44)))
                    WorldGen.GrowTree(x, y - 1);
            }
        }
        for(int edge=60;edge<p.Width-32;edge+=37)
        {
            int x=p.X(edge),y=p.Floor(edge);
            if(admission.Allows(new Rectangle(x-3,y-6,7,8))) WorldGen.PlaceTile(x,y-1,TileID.Coral,mute:true);
        }
        // Roadmap item 7: wrecks, ruins, coral gardens and kelp on the floor,
        // placed before the settle so the refill receipt covers them too.
        string seabed = DecorateSeabed(p, admission);
        if (p.IslandLayout >= 4) seabed += "|" + DressOpenFloor(p, admission);
        Mod owner = ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod;
        owner.Logger.Info("DINGDONG_OPEN_OCEAN_CONTENT|" + seabed);
        string settlement = SettleAndRefill(p, admission, owner);
        owner.Logger.Info(
            $"DINGDONG_OPEN_OCEAN|version=1|side={(p.Left ? "left" : "right")}|width={p.Width}|sea={p.SeaLevel}|bottom={p.Bottom}|underworldGap={p.Clearance}|waterCells={carved}|retainedCells={retained}|islands={p.Islands.Count}|airGrottos={p.Grottos.Count}|landmarks={p.Landmarks.Count}|grottoCandidates={grottoCandidates}|blockedGrottos={blockedGrottos}|adaptiveGrottos={adaptiveGrottos}|layoutSeed={p.Seed}|islandLayout={p.IslandLayout}|floorSealedCells={sealedCells}|damCells={damCells}|shellCells={shellCells}|{settlement}|abyssModified=False|orphanObjects={orphans}|embeddedRock={embedded}|keptProviderStructures={admission.Kept.Count}|carvedVanillaStructures={admission.DroppedVanillaComponents}|seamounts={seamountContent}");
        // A single grotto still gives the sea its breathing stop; the basin is
        // already carved here, so a missing second grotto is not worth a failed world.
        if(carved<(carved+retained)*.60||p.Grottos.Count<1)
            throw new InvalidOperationException($"Open ocean has insufficient unprotected space: water={carved}, retained={retained}, airGrottos={p.Grottos.Count}, candidates={grottoCandidates}, blocked={blockedGrottos}, layoutSeed={p.Seed}. Protected provider structures were preserved.");
    }
    private static ushort IsletMaterial(OceanExpeditionLayout p, OceanIsletColumn column, int y)
    {
        if (p.IslandLayout < 2)
            return y <= p.SeaLevel + 5 ? TileID.Sand : TileID.HardenedSand;
        // Beaches on the outer shore and on every harbour islet keep palm and
        // residence ground; the inner crown of a plain islet is soil under grass.
        bool beach = column.Harbor || column.Shore >= .58 || column.Top >= p.SeaLevel - 3;
        if (!beach && y < p.SeaLevel - 3)
            return y == column.Top ? TileID.Grass : TileID.Dirt;
        if (y <= p.SeaLevel + 5) return TileID.Sand;
        if (p.IslandLayout >= 3)
        {
            // Layout 3: the skin follows the slope (sand on the reef, coral stone in the twilight
            // sea, bare stone deeper); under the crown the sand thins out in a rounded lens.
            int below = y - column.Top;
            if (column.Shore >= 1)
            {
                int skin = 3 + (int)(2 * Math.Abs(Math.Sin(y * .07)));
                if (below < skin) return y < p.SeaLevel + 260 ? TileID.Sand : y < p.SeaLevel + 1300 ? TileID.Coralstone : TileID.Stone;
                return TileID.Stone;
            }
            double lens = 42 * Math.Sqrt(Math.Max(0, 1 - column.Shore * column.Shore)) + 4 * Math.Sin(y * .3);
            return y <= p.SeaLevel + lens ? TileID.HardenedSand : TileID.Stone;
        }
        return y <= p.SeaLevel + 40 ? TileID.HardenedSand : TileID.Stone;
    }
    // The pass runs after the last vanilla settlement, so the first settle happens
    // on world load and drains the basin into whatever it leaks into, leaving the
    // islets above the real surface. Settle here with native physics, top the
    // basin back up to its level and repeat until the refill becomes small.
    private const int SettleCycles = 3;
    private const int SettleSecondsPerCycle = 120;
    private static string SettleAndRefill(OceanExpeditionLayout p, Admission admission, Mod owner)
    {
        var report = new System.Text.StringBuilder("settleCycles=");
        int cycles = 0; long refilledTotal = 0; int surface = -1;
        for (int cycle = 1; cycle <= SettleCycles; cycle++)
        {
            cycles = cycle;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            Liquid.QuickWater(3);
            WorldGen.WaterCheck();
            bool previousQuick = Liquid.quickSettle;
            Liquid.quickSettle = true;
            try
            {
                while (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > 0 && clock.Elapsed.TotalSeconds < SettleSecondsPerCycle)
                    Liquid.UpdateLiquid();
            }
            finally { Liquid.quickSettle = previousQuick; }
            WorldGen.WaterCheck();
            surface = MeasureSurface(p);
            long inland = CountWater(p, p.Width, p.Width + 160, p.SeaLevel - 40, p.Bottom + 60);
            long mapEdge = CountWater(p, 0, 20, p.SeaLevel - 40, p.Bottom + 60);
            long below = CountWaterBelowFloor(p);
            long reserved = CountReservedWater(p, admission);
            long refilled = Refill(p, admission);
            refilledTotal += refilled;
            owner.Logger.Info($"DINGDONG_OPEN_OCEAN_SETTLE|cycle={cycle}|seconds={clock.Elapsed.TotalSeconds:F0}|surfaceBeforeRefill={surface}|target={p.SeaLevel}|refilledCells={refilled}|inlandWater={inland}|mapEdgeWater={mapEdge}|belowFloorWater={below}|reservedWater={reserved}|activeLiquid={Liquid.numLiquid}|buffered={LiquidBuffer.numLiquidBuffer}");
            if (refilled < 4000) break;
        }
        return report.Append(cycles).Append("|lastSurface=").Append(surface).Append("|refilledCells=").Append(refilledTotal).ToString();
    }

    // Median first-water row over the open middle of the basin.
    private static int MeasureSurface(OceanExpeditionLayout p)
    {
        var rows = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            int edge = (int)(p.Width * (.22 + .05 * i));
            if (p.IslandColumn(edge).Top <= p.SeaLevel + 200) continue;
            int x = p.X(edge);
            for (int y = Math.Max(1, p.SeaLevel - 120); y < p.Floor(edge); y++)
                if (Main.tile[x, y].LiquidAmount > 0 || Main.tile[x, y].HasTile) { rows.Add(y); break; }
        }
        if (rows.Count == 0) return -1;
        rows.Sort();
        return rows[rows.Count / 2];
    }

    // Where the sea goes when it settles: water outside the basin on the inland
    // side, in the untouched map-edge columns, or under the basin floor band.
    private static long CountWater(OceanExpeditionLayout p, int edgeFrom, int edgeTo, int yFrom, int yTo)
    {
        long count = 0;
        for (int edge = edgeFrom; edge < edgeTo; edge++)
        {
            int x = p.X(edge);
            if (x < 1 || x >= Main.maxTilesX - 1) continue;
            for (int y = Math.Max(1, yFrom); y < Math.Min(Main.maxTilesY - 1, yTo); y++)
                if (Main.tile[x, y].LiquidAmount > 0 && Main.tile[x, y].LiquidType == LiquidID.Water) count++;
        }
        return count;
    }

    // Water that ended up inside provider reservations within the basin volume
    // (sampled every fourth column): the only cells the shell cannot own.
    private static long CountReservedWater(OceanExpeditionLayout p, Admission admission)
    {
        long count = 0;
        for (int edge = 20; edge < p.Width; edge += 4)
        {
            int x = p.X(edge);
            for (int y = Math.Max(1, p.SeaLevel - 64); y < Math.Min(Main.maxTilesY - 1, p.Floor(edge) + 24); y++)
                if (!admission.Allows(x, y) && Main.tile[x, y].LiquidAmount > 0 && Main.tile[x, y].LiquidType == LiquidID.Water) count += 4;
        }
        return count;
    }

    private static long CountWaterBelowFloor(OceanExpeditionLayout p)
    {
        long count = 0;
        for (int edge = 20; edge < p.Width; edge += 4)
        {
            // Layout 3: the caves under the floor are meant to hold water; count what leaks under the seal.
            int x = p.X(edge), start = p.IslandLayout >= 3 ? p.Bottom : p.Floor(edge) + 24;
            for (int y = start; y < Math.Min(Main.maxTilesY - 1, start + 400); y++)
                if (Main.tile[x, y].LiquidAmount > 0 && Main.tile[x, y].LiquidType == LiquidID.Water) count += 4;
        }
        return count;
    }

    private static long Refill(OceanExpeditionLayout p, Admission admission)
    {
        long refilled = 0;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), floor = p.Floor(edge);
            var column = p.IslandColumn(edge);
            for (int y = p.SeaLevel; y < floor; y++)
            {
                if (!admission.Allows(x, y)) continue;
                if (y >= column.Top && y <= column.Bottom) continue;
                Tile t = Main.tile[x, y];
                if (t.HasTile && Main.tileSolid[t.TileType]) continue;
                if (t.LiquidAmount == 255 && t.LiquidType == LiquidID.Water) continue;
                if (t.LiquidAmount > 0 && t.LiquidType != LiquidID.Water) continue;
                bool grotto = false;
                foreach (var room in p.Grottos)
                    if (OceanExpeditionLayout.InsideEllipse(room.Bounds, x, y)) { grotto = true; break; }
                if (grotto) continue;
                t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
                refilled++;
            }
        }
        return refilled;
    }

    private static void CarveGrotto(OceanExpeditionLayout p, Rectangle b)
    {
        // An irregular-looking stone reef contains a dry ellipse. Native bubble
        // blocks are a walk-through liquid barrier, not a permanent refill loop.
        int floor = b.Center.Y + b.Height / 4;
        for (int x = b.Left; x < b.Right; x++)
        for (int y = b.Top; y < b.Bottom; y++)
        {
            if (!OceanExpeditionLayout.InsideEllipse(b, x, y)) continue;
            Tile t = Main.tile[x, y]; t.ClearEverything();
            if (!OceanExpeditionLayout.InsideEllipse(b, x, y, 7) || y >= floor)
            {
                t.HasTile = true; t.TileType = y >= floor && y < floor + 3 ? TileID.Coralstone : TileID.Stone;
            }
            else t.WallType = WallID.RocksUnsafe1;
        }
        // A side-facing bubble airlock joins this real continental cliff cave
        // to the sea. Its entrance admits players and excludes liquid.
        int barrier = p.Left ? b.Left+8 : b.Right-11;
        int mouthStart=p.Left?b.Left-8:b.Center.X+b.Width/6;
        int mouthEnd=p.Left?b.Center.X-b.Width/6:b.Right+8;
        for (int x = mouthStart; x <= mouthEnd; x++)
        for (int y = floor - 13; y <= floor - 2; y++)
        {
            Tile t = Main.tile[x, y]; t.ClearEverything();
            if (x >= barrier && x <= barrier + 2)
            { t.HasTile = true; t.TileType = TileID.Bubble; }
            else if (p.Left ? x < barrier : x > barrier+2) { t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; }
            else t.WallType = WallID.RocksUnsafe1;
        }
        for (int x = b.Center.X - b.Width / 3; x <= b.Center.X + b.Width / 3; x += 9)
        {
            WorldGen.PlaceTile(x, floor - 1, TileID.Torches, mute: true, forced: true, style: 3);
        }
        // Each grotto includes one finite exploration reward and a breathing stop.
        PlaceCache(b.Center.X + 14, floor, 3);
    }
    private static void PlaceLandmark(OceanLandmark mark, int seaLevel)
    {
        int x = mark.Floor.X, y = mark.Floor.Y;
        // Broken arches and a supported ruin floor; openings are several player
        // widths, and the reward is visible through the missing central roof.
        ushort material = mark.Kind == 0 ? TileID.WoodBlock : TileID.SandstoneBrick;
        if(mark.Kind>0)
            for(int dx=-28;dx<=28;dx++)
            for(int dy=4;dy<=22;dy++)
                if(dx*dx/(28d*28d)+(dy-4)*(dy-4)/(18d*18d)<=1)Solid(x+dx,y+dy,dy<9?TileID.Coralstone:TileID.Stone);
        for(int dx=-20;dx<=20;dx++)
        for(int dy=-14;dy<0;dy++)
        {
            Tile t=Main.tile[x+dx,y+dy];t.ClearEverything();
            if(y+dy>=seaLevel){t.LiquidType=LiquidID.Water;t.LiquidAmount=255;}
        }
        for (int dx = -20; dx <= 20; dx++)
        for (int dy = 0; dy <= 3; dy++) Solid(x + dx, y + dy, material);
        for (int dx = -17; dx <= 17; dx += 17)
        for (int dy = -13; dy < 0; dy++)
            if (dx != 0 || dy > -8) Solid(x + dx, y + dy, material);
        for (int dx = -20; dx <= 20; dx++)
            if (Math.Abs(dx) > 8) Solid(x + dx, y - 14, material);
        PlaceCache(x + 5, y, mark.Kind);
    }
    private static void Solid(int x, int y, ushort type)
    {
        Tile t = Main.tile[x, y]; t.ClearEverything(); t.HasTile = true; t.TileType = type;
    }
    private static void PlaceCache(int x, int floor, int kind)
    {
        for (int dx = 0; dx < 2; dx++)
        {
            Solid(x + dx, floor, TileID.SandstoneBrick);
            for (int dy = -2; dy < 0; dy++) { Main.tile[x + dx, floor + dy].ClearTile(); }
        }
        int chest = WorldGen.PlaceChest(x, floor - 1, TileID.Containers, true, 17);
        if (chest < 0 || Main.chest[chest] == null) return;
        var items = Main.chest[chest].item;
        int[] reward = { ModContent.ItemType<IslandNavigator>(), ModContent.ItemType<PelagicScale>(), ModContent.ItemType<HadalLens>(), ModContent.ItemType<GrottoGeode>() };
        items[0].SetDefaults(reward[Math.Clamp(kind, 0, 3)]); items[0].stack = kind == 0 ? 1 : 3;
        items[1].SetDefaults(ItemID.GillsPotion); items[1].stack = 3;
        items[2].SetDefaults(ItemID.FlipperPotion); items[2].stack = 3;
        items[3].SetDefaults(ItemID.GoldCoin); items[3].stack = 1 + Math.Clamp(kind, 0, 3);
    }

    // Layout 3: chests, signs and tile entities whose tiles the carve removed.
    private static long ClearOrphanedObjects(OceanExpeditionLayout p, Admission admission)
    {
        // Exactly the cells the carve rewrote: open columns from above the sea to just under the floor.
        bool Gone(int x, int y)
        {
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return false;
            int edge = p.Edge(x);
            return p.Owns(edge, y) && admission.Allows(x, y);
        }
        long count = 0;
        for (int i = 0; i < Main.maxChests; i++)
            if (Main.chest[i] is Chest c && Gone(c.x, c.y)) { Main.chest[i] = null; count++; }
        for (int i = 0; i < Main.sign.Length; i++)
            if (Main.sign[i] is Sign s && Gone(s.x, s.y)) { Main.sign[i] = null; count++; }
        foreach (var entry in Terraria.DataStructures.TileEntity.ByPosition.ToArray())
            if (Gone(entry.Key.X, entry.Key.Y))
            {
                Terraria.DataStructures.TileEntity.ByPosition.Remove(entry.Key);
                Terraria.DataStructures.TileEntity.ByID.Remove(entry.Value.ID);
                count++;
            }
        return count;
    }

    // Layout 3: a provider structure left inside the sea is grown into the sea floor:
    // an irregular rock mantle around its preserved shell and a flaring seamount
    // below it down to the floor, so nothing hangs in open water.
    private static long EmbedProviderStructures(OceanExpeditionLayout p, Admission admission)
    {
        long cells = 0;
        bool Open(int x, int y)
        {
            if (!admission.Allows(x, y)) return false;
            Tile t = Main.tile[x, y];
            return !t.HasTile && y >= p.SeaLevel;
        }
        void Rock(int x, int y) { Tile t = Main.tile[x, y]; t.ClearEverything(); t.HasTile = true; t.TileType = TileID.Stone; cells++; }
        double Noise(int x, int y) => .5 + .25 * Math.Sin(x * .061 + y * .027 + p.Seed % 97) + .25 * Math.Sin(y * .083 - x * .019 + p.Seed % 41);
        foreach (Rectangle kept in admission.Kept)
        {
            Rectangle shell = kept; shell.Inflate(16, 16);
            int pad = 46;
            for (int x = Math.Max(2, shell.Left - pad); x < Math.Min(Main.maxTilesX - 2, shell.Right + pad); x++)
            {
                int edge = p.Edge(x);
                if (edge < 20 || edge >= p.Width) continue;
                int floor = p.Floor(edge);
                for (int y = Math.Max(p.SeaLevel, shell.Top - pad); y < Math.Min(floor, shell.Bottom + pad); y++)
                {
                    int dx = x < shell.Left ? shell.Left - x : x >= shell.Right ? x - shell.Right + 1 : 0;
                    int dy = y < shell.Top ? shell.Top - y : y >= shell.Bottom ? y - shell.Bottom + 1 : 0;
                    if (Math.Sqrt(dx * dx + dy * dy) <= 14 + 30 * Noise(x, y) && Open(x, y)) Rock(x, y);
                }
            }
            // Seamount under the structure, flaring with depth.
            int centre = kept.Center.X, top = shell.Bottom + 10;
            int floorHere = p.Floor(p.Edge(Math.Clamp(centre, 1, Main.maxTilesX - 2)));
            for (int y = top; y < floorHere + 24 && y < Main.maxTilesY - 200; y++)
            {
                double half = kept.Width * .5 + 30 + (y - top) * .09 + 24 * Noise(centre, y);
                for (int x = (int)(centre - half); x <= (int)(centre + half); x++)
                {
                    if (x < 2 || x >= Main.maxTilesX - 2) continue;
                    int edge = p.Edge(x);
                    if (edge < 20 || edge >= p.Width || y >= p.Floor(edge)) continue;
                    // Layout 4: a background column joins the structure to the floor instead of rock.
                    if (p.IslandLayout >= 4) { if (Open(x, y)) Main.tile[x, y].WallType = p.GhostWall(edge, y); }
                    else if (Open(x, y)) Rock(x, y);
                }
            }
        }
        return cells;
    }

    // Layout 3: the seamounts are not bare rock. Each carries ore and gem veins in its body and
    // two flooded caves at different depths, each with a reward cache; RNG is isolated so the
    // rest of the world stays the same.
    // O2 render: the seamounts read as flat grey because the slope skin was laid per column
    // and a near-vertical flank has one top cell per column. Paint the stone within a few
    // tiles of open water, both across and down: hardened sand on the reef, coral stone in
    // the twilight sea, mud on the deep stem. None of them falls when a ledge overhangs.
    private static long SkinSeamounts(OceanExpeditionLayout p, Admission admission)
    {
        long painted = 0;
        int top = p.SeaLevel + 6, bottom = p.SeaLevel + 8;
        // Islet stems end at the open floor; nothing below it can be islet stone.
        for (int edge = 20; edge < p.Width; edge++) bottom = Math.Max(bottom, p.Floor(edge) + 30);
        bottom = Math.Min(bottom, Math.Min(p.Bottom, Main.maxTilesY - 2));
        bool Rock(int edge, int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.TileType != TileID.Stone) return false;
            var column = p.IslandColumn(edge);
            return y >= column.Top && y <= column.Bottom && y < p.Floor(edge) && admission.Allows(x, y);
        }
        int Thickness(int x, int y) => 3 + (int)((uint)(x * 7349 ^ y * 1931) % 3u);
        ushort Skin(int y) => y < p.SeaLevel + 240 ? TileID.HardenedSand : y < p.SeaLevel + 1300 ? TileID.Coralstone : TileID.Mud;
        void Paint(int x, int y) { Main.tile[x, y].TileType = Skin(y); painted++; }
        var near = new int[p.Width];
        for (int y = top; y < bottom; y++)
        {
            int run = 99;
            for (int edge = 20; edge < p.Width; edge++)
            {
                run = Main.tile[p.X(edge), y].HasTile ? Math.Min(run + 1, 99) : 0;
                near[edge] = run;
            }
            run = 99;
            for (int edge = p.Width - 1; edge >= 20; edge--)
            {
                int x = p.X(edge);
                run = Main.tile[x, y].HasTile ? Math.Min(run + 1, 99) : 0;
                if (Math.Min(run, near[edge]) <= Thickness(x, y) && Rock(edge, x, y)) Paint(x, y);
            }
        }
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), run = 99;
            for (int y = top; y < bottom; y++)
            {
                run = Main.tile[x, y].HasTile ? Math.Min(run + 1, 99) : 0;
                if (run <= Thickness(x, y) && Rock(edge, x, y)) Paint(x, y);
            }
        }
        return painted;
    }

    // Rock of the cave mass: coral stone and clay near the open floor, mud and stone in the middle,
    // stone with obsidian lenses just above the Underworld seal. No sand family (it would count as
    // a desert) and no Calamity tiles (they would turn the sea into the Abyss biome).
    private static ushort CaveRock(OceanExpeditionLayout p, int edge, int y, int floor)
    {
        double depth = (y - floor) / (double)Math.Max(1, p.Bottom - floor);
        double m = p.Fbm(edge / 150.0, y / 60.0, 23);
        if (depth < .3) return m < .45 ? TileID.Coralstone : m < .72 ? TileID.Stone : TileID.ClayBlock;
        if (depth < .7) return m < .4 ? TileID.Mud : m < .76 ? TileID.Stone : TileID.ClayBlock;
        return m < .5 ? TileID.Stone : m < .82 ? TileID.Mud : depth > .9 && m > .9 ? TileID.Obsidian : TileID.Stone;
    }

    // Owner, 29.10: "resources only along the way down — they must be everywhere". Ore and gem
    // veins on a grid through the whole cave mass, and on the cave floors caches, life crystals,
    // pots and coral, all by depth. Isolated RNG: later passes keep their exact random stream.
    private static string DecorateCaves(OceanExpeditionLayout p, Admission admission)
    {
        if (p.IslandLayout < 3) return "none";
        int veins = 0, chests = 0, crystals = 0, pots = 0, coral = 0;
        ushort[] shallow = { TileID.Copper, TileID.Tin, TileID.Iron, TileID.Lead, TileID.Sapphire, TileID.Emerald };
        ushort[] middle = { TileID.Silver, TileID.Tungsten, TileID.Gold, TileID.Platinum, TileID.Ruby, TileID.Topaz, TileID.Amethyst };
        ushort[] deep = { TileID.Gold, TileID.Platinum, TileID.Diamond, TileID.Ruby, TileID.Tungsten, TileID.Silver };
        var random = new Random(p.Seed ^ 0xCA7E5);
        IsolatedGenRand.Run(p.Seed ^ 0x4C0FE, () =>
        {
            for (int edge = 80; edge < p.Width - 110; edge += 70)
            {
                int floor = p.Floor(edge);
                for (int y = floor + 60; y < p.Bottom - 70; y += 70)
                {
                    int e = edge + random.Next(-25, 26), yy = y + random.Next(-25, 26);
                    int x = p.X(e);
                    Tile t = Main.tile[x, yy];
                    // A TileRunner vein wanders up to ~30 tiles: keep it clear of every protected cell.
                    if (!t.HasTile || !Main.tileSolid[t.TileType] || !p.Owns(e, yy) || !admission.Allows(new Rectangle(x - 40, yy - 40, 81, 81))) continue;
                    double depth = (yy - floor) / (double)Math.Max(1, p.Bottom - floor);
                    ushort[] set = depth < .3 ? shallow : depth < .7 ? middle : deep;
                    WorldGen.TileRunner(x, yy, random.Next(5, 11), random.Next(8, 20), set[random.Next(set.Length)]);
                    veins++;
                }
            }
            for (int edge = 120; edge < p.Width - 140; edge += 230)
            {
                int floor = p.Floor(edge);
                for (int y = floor + 80; y < p.Bottom - 120; y += 190)
                {
                    int e = edge + random.Next(-90, 91), x = p.X(e);
                    // Down from a random point to the first cave floor: open water over solid rock.
                    int top = y + random.Next(0, 120), found = -1;
                    for (int yy = top; yy < Math.Min(top + 160, p.Bottom - CaveSealRows); yy++)
                        if (Main.tile[x, yy].LiquidAmount > 0 && !Main.tile[x, yy].HasTile &&
                            Main.tile[x, yy + 1].HasTile && Main.tileSolid[Main.tile[x, yy + 1].TileType]) { found = yy; break; }
                    if (found < 0 || !p.Owns(e, found) || !admission.Allows(new Rectangle(x - 6, found - 6, 13, 10)) || !OceanContentKit.NoChestNear(x, found, 40)) continue;
                    double depth = (found - floor) / (double)Math.Max(1, p.Bottom - floor);
                    int roll = random.Next(100);
                    if (roll < 34) { PlaceCache(x, found + 1, depth < .35 ? 1 : depth < .7 ? 2 : 3); chests++; }
                    else if (roll < 48) { if (WorldGen.AddLifeCrystal(x, found)) crystals++; }
                    else if (roll < 74)
                    {
                        for (int k = -6; k <= 6; k += 3)
                        {
                            int px = x + k;
                            if (!Main.tile[px, found].HasTile && Main.tile[px, found + 1].HasTile && Main.tileSolid[Main.tile[px, found + 1].TileType] &&
                                WorldGen.PlacePot(px, found, 28, random.Next(4))) pots++;
                        }
                    }
                    for (int k = -10; k <= 10; k += 2)
                    {
                        int px = x + k;
                        if (!Main.tile[px, found].HasTile && Main.tile[px, found + 1].HasTile && Main.tileSolid[Main.tile[px, found + 1].TileType] && random.Next(3) == 0)
                        { WorldGen.PlaceTile(px, found, TileID.Coral, mute: true); if (Main.tile[px, found].HasTile) coral++; }
                    }
                    WorldGen.RangeFrame(x - 12, found - 6, x + 12, found + 3);
                }
            }
        });
        return $"veins:{veins},caches:{chests},lifeCrystals:{crystals},pots:{pots},coral:{coral}";
    }
    private const int CaveSealRows = OceanExpeditionLayout.CaveSeal + 2;

    // Layout 4: hanging reef ledges inside each islet's wall cone, every 55-90 rows from under the
    // keel to above the floor: coral stone with a sand skin, seaweed and coral on top, a coral torch,
    // pots on some ledges and one depth-tiered cache per islet. Backed by the cone's walls, so a
    // ledge reads as part of the reef, not a block hanging in the sea.
    private static string DecorateGhostStems(OceanExpeditionLayout p, Admission admission)
    {
        OceanModDecor.Resolve();
        int ledges = 0, chests = 0, pots = 0, plants = 0, lights = 0;
        var random = new Random(p.Seed ^ 0x6057AE);
        IsolatedGenRand.Run(p.Seed ^ 0x60577E, () =>
        {
            foreach (var island in p.Islands)
            {
                int centre = island.Edge, floor = p.Floor(centre), top = p.GhostTop(centre);
                if (top >= floor - 90) continue;
                bool cache = false; int n = 0;
                for (int y = top + 40 + random.Next(20); y < floor - 45; y += 55 + random.Next(36), n++)
                {
                    int l = centre, r = centre;
                    while (l > 21 && p.InGhost(l - 1, y)) l--;
                    while (r < p.Width - 2 && p.InGhost(r + 1, y)) r++;
                    int span = r - l;
                    if (span < 40) continue;
                    int len = (int)(span * (.28 + .27 * random.NextDouble()));
                    int start = n % 2 == 0 ? l + 6 + random.Next(Math.Max(1, span / 6)) : r - 6 - len - random.Next(Math.Max(1, span / 6));
                    var tops = new List<(int Edge, int Row)>();
                    for (int e = start; e <= start + len; e++)
                    {
                        double t = (e - start) / (double)Math.Max(1, len);
                        int row = y + (int)Math.Round(Math.Sin(e * .12 + n) * 1.5);
                        int thick = Math.Max(1, (int)(Math.Sin(t * Math.PI) * (3 + 2 * Math.Abs(Math.Sin(e * .31)))));
                        int x = p.X(e);
                        bool ok = true;
                        for (int k = -1; k <= thick && ok; k++) ok = p.InGhost(e, row + k) && admission.Allows(x, row + k);
                        if (!ok) continue;
                        for (int k = 0; k < thick; k++) Solid(x, row + k, k == 0 && thick > 1 ? TileID.Sand : TileID.Coralstone);
                        tops.Add((e, row));
                    }
                    if (tops.Count < 12) continue;
                    ledges++;
                    foreach (var (e, row) in tops)
                    {
                        int x = p.X(e);
                        if (Main.tile[x, row].TileType != TileID.Sand) continue;
                        // Mod decor (Calamity corals, Spirit kelp and corals when installed), else vanilla.
                        if (random.Next(3) > 0) plants += OceanModDecor.Dress(x, row, true, (row - p.SeaLevel) / (double)p.Depth, random);
                        else if (random.Next(2) == 0) plants += Coral(x, row) ? 1 : 0;
                    }
                    var (le, lr) = tops[tops.Count / 3];
                    int lx = p.X(le);
                    if (!Main.tile[lx, lr - 1].HasTile)
                    {
                        WorldGen.PlaceTile(lx, lr - 1, TileID.Torches, mute: true, forced: true, style: TorchID.Coral);
                        if (Main.tile[lx, lr - 1].HasTile && Main.tile[lx, lr - 1].TileType == TileID.Torches) lights++;
                    }
                    if (random.Next(2) == 0)
                    {
                        var (pe, pr) = tops[tops.Count * 2 / 3];
                        int px = p.X(pe);
                        if (!Main.tile[px, pr - 1].HasTile && WorldGen.PlacePot(px, pr - 1, 28, random.Next(4))) pots++;
                    }
                    // One cache per islet, on the ledge closest to mid-depth.
                    if (!cache && y > top + (floor - top) / 3)
                    {
                        var (ce, cr) = tops[tops.Count / 2];
                        int cx = p.X(ce);
                        if (admission.Allows(new Rectangle(cx - 2, cr - 4, 5, 6)) && OceanContentKit.NoChestNear(cx, cr, 40))
                        {
                            int depth = cr - p.SeaLevel;
                            PlaceCache(cx, cr, depth < 160 ? 1 : depth < 320 ? 2 : 3); chests++; cache = true;
                        }
                    }
                    int x0 = Math.Min(p.X(tops[0].Edge), p.X(tops[^1].Edge)), x1 = Math.Max(p.X(tops[0].Edge), p.X(tops[^1].Edge));
                    WorldGen.RangeFrame(x0 - 2, y - 14, x1 + 2, y + 8);
                }
            }
        });
        return $"ghostLedges:{ledges},ghostCaches:{chests},ghostPots:{pots},ghostPlants:{plants},ghostLights:{lights}";
    }

    private static string DecorateSeamounts(OceanExpeditionLayout p, Admission admission)
    {
        int veins = 0, caves = 0, caches = 0;
        ushort[] ores = { TileID.Gold, TileID.Platinum, TileID.Sapphire, TileID.Emerald, TileID.Ruby, TileID.Diamond, TileID.Silver, TileID.Tungsten };
        var random = new Random(p.Seed ^ 0x5EA4A0);
        IsolatedGenRand.Run(p.Seed ^ 0x3A40E7, () =>
        {
            foreach (var island in p.Islands)
            {
                // Ore and gem veins inside the solid body.
                for (int k = 0; k < 30; k++)
                {
                    int edge = island.Edge + random.Next(-island.Radius - 120, island.Radius + 121);
                    if (edge < 20 || edge >= p.Width) continue;
                    var column = p.IslandColumn(edge);
                    int floor = p.Floor(edge);
                    if (column.Top >= floor - 40) continue;
                    int y = random.Next(Math.Max(p.SeaLevel + 60, column.Top + 18), floor - 10);
                    int x = p.X(edge);
                    if (!admission.Allows(new Rectangle(x - 40, y - 40, 81, 81)) || !Main.tile[x, y].HasTile) continue;
                    WorldGen.TileRunner(x, y, random.Next(6, 12), random.Next(10, 24), ores[random.Next(ores.Length)]);
                    veins++;
                }
                // Two flooded caves bored into the flank down the stem, each with a cache.
                int stem = p.Floor(island.Edge) - p.SeaLevel;
                foreach (double depthShare in new[] { .35, .72 })
                {
                    int targetY = p.SeaLevel + (int)(stem * depthShare) + random.Next(-20, 21);
                    int side = random.Next(2) == 0 ? -1 : 1;
                    int edge = island.Edge;
                    // Walk outward to the flank surface at that depth, then 30 tiles inside it.
                    int surface = -1;
                    for (int e = island.Edge; e >= 20 && e < p.Width && Math.Abs(e - island.Edge) < 2000; e += side)
                        if (p.IslandColumn(e).Top > targetY) { surface = e; break; }
                    if (surface < 0) continue;
                    edge = surface - side * 30;
                    int cx = p.X(edge), cy = targetY;
                    Rectangle box = new(cx - 26, cy - 14, 53, 29);
                    if (!admission.Allows(box)) continue;
                    for (int x = box.Left; x < box.Right; x++)
                    for (int y = box.Top; y < box.Bottom; y++)
                    {
                        double dx = (x - cx) / 26d, dy = (y - cy) / 13d;
                        if (dx * dx + dy * dy > 1 + .18 * Math.Sin(x * .5 + y * .7)) continue;
                        Tile t = Main.tile[x, y]; t.ClearEverything(); t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
                    }
                    // Tunnel from the cave to open water.
                    int mouthX = p.X(surface + side * 6);
                    for (int x = Math.Min(cx, mouthX); x <= Math.Max(cx, mouthX); x++)
                    for (int y = cy - 4; y <= cy + 4; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (!admission.Allows(x, y)) continue;
                        t.ClearEverything(); t.LiquidType = LiquidID.Water; t.LiquidAmount = 255;
                    }
                    int floorY = cy + 11;
                    for (int x = cx - 4; x <= cx + 4; x++) Solid(x, floorY, TileID.Coralstone);
                    PlaceCache(cx - 1, floorY, depthShare < .3 ? 1 : 2);
                    caches++; caves++;
                    WorldGen.RangeFrame(box.Left - 2, box.Top - 2, box.Right + 2, box.Bottom + 2);
                }
            }
        });
        return $"veins:{veins},caves:{caves},caches:{caches}";
    }
}
