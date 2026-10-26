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
    internal static void Apply(OceanExpeditionLayout p)
    {
        Admission admission = new(p);
        long carved = 0, retained = 0;
        for (int edge = 20; edge < p.Width; edge++)
        {
            int x = p.X(edge), floor = p.Floor(edge); var islandSpan=p.IslandSpan(edge);
            for (int y = p.SeaLevel - 64; y < floor + 24; y++)
            {
                if (!admission.Allows(x, y)) { retained++; continue; }
                Tile t = Main.tile[x, y];
                t.ClearEverything();
                bool island = y>=islandSpan.Top&&y<=islandSpan.Bottom;
                if (island || y >= floor)
                {
                    t.HasTile = true;
                    t.TileType = island ? (y <= p.SeaLevel + 5 ? TileID.Sand : TileID.HardenedSand) :
                        y < floor + 4 ? TileID.Sand : TileID.Stone;
                }
                else if (y >= p.SeaLevel) { t.LiquidType = LiquidID.Water; t.LiquidAmount = 255; carved++; }
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
        }
        for(int edge=60;edge<p.Width-32;edge+=37)
        {
            int x=p.X(edge),y=p.Floor(edge);
            if(admission.Allows(new Rectangle(x-3,y-6,7,8))) WorldGen.PlaceTile(x,y-1,TileID.Coral,mute:true);
        }
        ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod.Logger.Info(
            $"DINGDONG_OPEN_OCEAN|version=1|side={(p.Left ? "left" : "right")}|width={p.Width}|sea={p.SeaLevel}|bottom={p.Bottom}|underworldGap={p.Clearance}|waterCells={carved}|retainedCells={retained}|islands={p.Islands.Count}|airGrottos={p.Grottos.Count}|landmarks={p.Landmarks.Count}|grottoCandidates={grottoCandidates}|blockedGrottos={blockedGrottos}|adaptiveGrottos={adaptiveGrottos}|layoutSeed={p.Seed}|abyssModified=False");
        if(carved<(carved+retained)*.60||p.Grottos.Count<2)
            throw new InvalidOperationException($"Open ocean has insufficient unprotected space: water={carved}, retained={retained}, airGrottos={p.Grottos.Count}, candidates={grottoCandidates}, blocked={blockedGrottos}, layoutSeed={p.Seed}. Protected provider structures were preserved.");
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
