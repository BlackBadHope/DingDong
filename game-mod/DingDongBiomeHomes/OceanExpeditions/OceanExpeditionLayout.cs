using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.OceanExpeditions;

public enum OceanExpeditionZone : byte { None, Shelf, OpenSea, DeepSea, HadalSea, AirGrotto }
public readonly record struct OceanIslet(int Edge, int Radius, int Crown, int Keel, bool Harbor = false);
public readonly record struct OceanGrotto(Rectangle Bounds);
public readonly record struct OceanLandmark(Point Floor, int Kind);

// The sea is a volume measured against the Underworld, never a multiplied
// vanilla puddle. All positions are deterministic and mirrored around the coast.
public sealed class OceanExpeditionLayout
{
    public const int Version = 1;
    public int WorldWidth { get; }
    public int WorldHeight { get; }
    public bool Left { get; }
    public int Width { get; }
    public int SeaLevel { get; }
    public int Bottom { get; }
    public int Seed { get; }
    public int Depth => Bottom - SeaLevel;
    public int Clearance => 100;
    public List<OceanIslet> Islands { get; } = new();
    public List<OceanGrotto> Grottos { get; } = new();
    public List<OceanLandmark> Landmarks { get; } = new();
    public Rectangle Bounds => new(Left ? 20 : WorldWidth - Width, SeaLevel - 64, Width - 20, Depth + 96);

    public OceanExpeditionLayout(int worldWidth, int worldHeight, bool left, int seaLevel, int underworld, int seed)
    {
        if (worldWidth is < 1200 or > 32000 || worldHeight is < 600 or > 12000 || seaLevel < 100 ||
            seaLevel >= worldHeight - 360 || underworld > worldHeight - 80 || underworld - seaLevel < 360)
            throw new ArgumentOutOfRangeException(nameof(worldWidth), "Ocean needs a complete surface-to-Underworld envelope.");
        WorldWidth = worldWidth; WorldHeight = worldHeight; Left = left;
        Width = Math.Clamp(worldWidth / 6, 200, worldWidth / 5);
        SeaLevel = seaLevel; Bottom = Math.Min(worldHeight - 160, underworld - Clearance); Seed = seed;
        if (Depth < 260) throw new ArgumentOutOfRangeException(nameof(underworld));
        Random random = new(seed);
        int count = Math.Clamp(Width / 650, 3, 7);
        for (int i = 0; i < count; i++)
        {
            int edge = (int)(Width * (.13 + .60 * i / Math.Max(1, count - 1)));
            int radius = Math.Clamp(Width / (count * 13), 24, 90) + random.Next(0, 12);
            // Three coastal settlement sites share the six shore residences.
            // Their shallow keels leave the deep swimming volume open; their
            // surface footprint still leaves at least 72% of the sea exposed.
            bool harbor = count >= 5 ? i is 0 or 2 or 4 : i == count / 2;
            if(harbor)radius=Math.Max(radius,Math.Clamp(Width/30,60,124));
            int crown=random.Next(12,29);
            int keel=harbor?random.Next(85,125):random.Next(90,180)+(i%2)*Depth/20;
            Islands.Add(new(edge,radius,crown,keel,harbor));
        }
        int grottos = Math.Clamp(Depth / 850, 3, 6);
        for (int i = 0; i < grottos; i++)
        {
            float fraction = .22f + .63f * i / Math.Max(1, grottos - 1);
            int rx = Math.Clamp(Width / 22, 45, 128), ry = Math.Clamp(Depth / 60, 24, 58);
            int y = SeaLevel + (int)(Depth * fraction);
            int edge = CliffEdge(y) + rx / 3;
            Grottos.Add(new(new(X(edge) - rx, y - ry, rx * 2 + 1, ry * 2 + 1)));
        }
        // Landmarks are split between the islands, midwater ledges and the deep floor.
        // Exploration caches must not reserve the ground intended for homes.
        foreach (var island in Islands)
            if(!island.Harbor)Landmarks.Add(new(new(X(island.Edge), SeaLevel - island.Crown), 0));
        for (int i = 0; i < 8; i++)
        {
            int edge = (int)(Width * (.12 + .60 * ((i * 3) % 8) / 7d));
            int y = i < 4 ? SeaLevel + (int)(Depth * (.22 + i * .16)) : Floor(edge) - 8;
            Landmarks.Add(new(new(X(edge), y), i % 3 + 1));
        }
    }
    public int X(int edge) => Left ? edge : WorldWidth - 1 - edge;
    public int Edge(int x) => Left ? x : WorldWidth - 1 - x;
    public int Floor(int edge)
    {
        double t = Math.Clamp((double)edge / Width, 0, 1);
        // 65% of the width has near-full depth; the inland 17% is a short shelf.
        double deep = t < .65 ? .97 : t < .83 ? .97 - (t - .65) / .18 * .75 : .22 * Math.Pow((1 - t) / .17, 1.5);
        double relief = Math.Sin(edge * .013 + (Seed & 255)) * 9 + Math.Sin(edge * .037) * 4;
        if (t < .65) relief -= Math.Pow(Math.Max(0, Math.Sin(edge * .004 + (Seed & 15))), 8) * Depth * .06;
        return Math.Clamp(SeaLevel + (int)(Depth * deep + relief), SeaLevel + 4, Bottom);
    }
    public int CliffEdge(int y)
    {
        int low = (int)(Width * .65), high = Width - 1;
        while (high - low > 1) { int mid = (low + high) / 2; if (Floor(mid) > y) low = mid; else high = mid; }
        return low;
    }
    public (int Top,int Bottom) IslandSpan(int edge)
    {
        foreach (var i in Islands)
        {
            double d = Math.Abs(edge-i.Edge)/(double)i.Radius;
            if (d>=1)continue;
            return (SeaLevel-(int)(i.Crown*Math.Pow(1-d*d,.65)),SeaLevel+(int)(i.Keel*Math.Pow(1-d*d,.55)));
        }
        return (int.MaxValue,int.MinValue);
    }
    public int IslandFloor(int edge)
    {
        foreach (var i in Islands)
        {
            double d = Math.Abs(edge - i.Edge) / (double)i.Radius;
            if (d < 1) return SeaLevel - (int)(i.Crown * Math.Pow(1 - d * d, .65));
        }
        return int.MaxValue;
    }
    public bool IslandSolid(int edge, int y)
    {
        foreach (var i in Islands)
        {
            double d = Math.Abs(edge - i.Edge) / (double)i.Radius;
            if (d >= 1) continue;
            int top = SeaLevel - (int)(i.Crown * Math.Pow(1 - d * d, .65));
            int bottom = SeaLevel + (int)(i.Keel * Math.Pow(1 - d * d, .55));
            if (y >= top && y <= bottom) return true;
        }
        return false;
    }
    public OceanExpeditionZone Zone(int x, int y)
    {
        int edge = Edge(x);
        if (edge < 20 || edge >= Width || y < SeaLevel - 48 || y >= Bottom + 20) return OceanExpeditionZone.None;
        foreach (var room in Grottos)
            if (InsideEllipse(room.Bounds, x, y, 8)) return OceanExpeditionZone.AirGrotto;
        if (y > Floor(edge) || IslandSolid(edge, y)) return OceanExpeditionZone.None;
        float depth = (y - SeaLevel) / (float)Depth;
        return depth >= .72f ? OceanExpeditionZone.HadalSea : depth >= .30f ? OceanExpeditionZone.DeepSea :
            edge > Width * .83 ? OceanExpeditionZone.Shelf : OceanExpeditionZone.OpenSea;
    }
    public static bool InsideEllipse(Rectangle b, int x, int y, int inset = 0)
    {
        double rx = b.Width * .5 - inset, ry = b.Height * .5 - inset;
        if (rx <= 0 || ry <= 0) return false;
        double dx = (x - b.Center.X) / rx, dy = (y - b.Center.Y) / ry;
        return dx * dx + dy * dy <= 1;
    }
}
