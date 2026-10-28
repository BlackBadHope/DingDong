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
        internal Admission(OceanExpeditionLayout p)
        {
            bool[] terrain = OceanTerrainPolicy.Create();
            bool[] structureOnly = ContinentalPlacementMaskPolicy.CreateStructureOnly();
            for (int edge = 16; edge < p.Width + 32; edge += 16)
            for (int y = p.SeaLevel - 160; y < p.Bottom + 48; y += 16)
            {
                int x = p.Left ? edge : p.WorldWidth - edge - 16;
                Rectangle b = Rectangle.Intersect(new(x, y, 16, 16), new(1, 1, p.WorldWidth - 2, p.WorldHeight - 2));
                bool deny = !GenVars.structures.CanPlace(b, structureOnly, 12);
                if (!deny)
                {
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
                // Align cells to world coordinates, including mirrored coasts.
                for (int xx = (b.Left - 16) / 16; xx <= (b.Right + 16) / 16; xx++)
                for (int yy = (b.Top - 16) / 16; yy <= (b.Bottom + 16) / 16; yy++) blocked.Add(new(xx, yy));
            }
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
        long carved = 0, retained = 0;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), floor = p.Floor(edge); var column=p.IslandColumn(edge);
            for (int y = p.SeaLevel - 64; y < floor + 24; y++)
            {
                if (!admission.Allows(x, y)) { retained++; continue; }
                Tile t = Main.tile[x, y];
                t.ClearEverything();
                bool island = y>=column.Top&&y<=column.Bottom;
                if (island || y >= floor)
                {
                    t.HasTile = true;
                    t.TileType = island ? IsletMaterial(p, column, y) :
                        y < floor + FloorSandDepth(edge) ? TileID.Sand : TileID.Stone;
                }
                else if (y >= p.SeaLevel) { t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; carved++; }
            }
        }
        // The basin floor now sits in the cavern layer. Natural caves right under
        // the floor band drain the sea when the world settles, so plug them with
        // stone down to the first solid run; provider reservations stay untouched.
        long sealedCells = 0;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), start = p.Floor(edge) + 24, solidRun = 0;
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
        for (int edge = 1; edge < p.Width + DamWidth; edge++)
        {
            if (edge >= 20 && edge < p.Width) continue;
            int x = p.X(edge);
            if (x < 1 || x >= Main.maxTilesX - 1) continue;
            for (int y = p.SeaLevel - 8; y < p.Bottom + 24 && y < Main.maxTilesY - 200; y++)
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
            int bottom = edge >= 20 && edge < p.Width ? p.Floor(edge) + 24 : p.Bottom + 24;
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
        foreach (var room in rooms)
        {
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
                int edge=p.CliffEdge(cy)+w/6+shift;
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
        Mod owner = ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod;
        owner.Logger.Info("DINGDONG_OPEN_OCEAN_CONTENT|" + seabed);
        string settlement = SettleAndRefill(p, admission, owner);
        owner.Logger.Info(
            $"DINGDONG_OPEN_OCEAN|version=1|side={(p.Left ? "left" : "right")}|width={p.Width}|sea={p.SeaLevel}|bottom={p.Bottom}|underworldGap={p.Clearance}|waterCells={carved}|retainedCells={retained}|islands={p.Islands.Count}|airGrottos={p.Grottos.Count}|landmarks={p.Landmarks.Count}|grottoCandidates={grottoCandidates}|blockedGrottos={blockedGrottos}|adaptiveGrottos={adaptiveGrottos}|layoutSeed={p.Seed}|islandLayout={p.IslandLayout}|floorSealedCells={sealedCells}|damCells={damCells}|shellCells={shellCells}|{settlement}|abyssModified=False");
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
            int x = p.X(edge), start = p.Floor(edge) + 24;
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
}
