using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.OceanExpeditions;

public enum OceanExpeditionZone : byte { None, Shelf, OpenSea, DeepSea, HadalSea, AirGrotto }
public readonly record struct OceanIslet(int Edge, int Radius, int Crown, int Keel, bool Harbor = false);
public readonly record struct OceanGrotto(Rectangle Bounds);
public readonly record struct OceanLandmark(Point Floor, int Kind);
// Shore is the normalized distance from the islet centre: 0 at the centre, 1 at the waterline.
public readonly record struct OceanIsletColumn(int Top, int Bottom, double Shore, bool Harbor);

// The sea is a volume measured against the Underworld, never a multiplied
// vanilla puddle. All positions are deterministic and mirrored around the coast.
public sealed class OceanExpeditionLayout
{
    public const int Version = 1;
    // Layout 2: the basin stops at a fraction of the world height instead of the
    // Underworld ceiling, and islets are fewer, irregularly spaced and vegetated.
    // Layout 3 (v0.3, owner 28.10): the sea floor falls from a coastal shelf down a
    // continental slope to an abyssal plain just above the Underworld, and every
    // islet is the peak of a seamount rooted on that floor instead of a hanging bowl.
    public const int CurrentIslandLayout = 3;
    public int WorldWidth { get; }
    public int WorldHeight { get; }
    public bool Left { get; }
    public int Width { get; }
    public int SeaLevel { get; }
    public int Bottom { get; }
    public int Seed { get; }
    public int IslandLayout { get; }
    public int Depth => Bottom - SeaLevel;
    public int Clearance => 100;
    public List<OceanIslet> Islands { get; } = new();
    public List<OceanGrotto> Grottos { get; } = new();
    public List<OceanLandmark> Landmarks { get; } = new();
    public Rectangle Bounds => new(Left ? 20 : WorldWidth - Width, SeaLevel - 64, Width - 20, Depth + 96);

    public OceanExpeditionLayout(int worldWidth, int worldHeight, bool left, int seaLevel, int underworld, int seed,
        int bottom = -1, int islandLayout = CurrentIslandLayout)
    {
        if (worldWidth is < 1200 or > 32000 || worldHeight is < 600 or > 12000 || seaLevel < 100 ||
            seaLevel >= worldHeight - 360 || underworld > worldHeight - 80 || underworld - seaLevel < 360)
            throw new ArgumentOutOfRangeException(nameof(worldWidth), "Ocean needs a complete surface-to-Underworld envelope.");
        WorldWidth = worldWidth; WorldHeight = worldHeight; Left = left;
        Width = Math.Clamp(worldWidth / 6, 200, worldWidth / 5);
        SeaLevel = seaLevel; Seed = seed; IslandLayout = islandLayout;
        int ceiling = Math.Min(worldHeight - 160, underworld - Clearance);
        Bottom = bottom >= 0 ? Math.Min(bottom, ceiling) : islandLayout >= 3 ? ceiling : SeaLevel + BasinDepth(worldHeight, ceiling - seaLevel);
        if (Depth < 260) throw new ArgumentOutOfRangeException(nameof(underworld));
        Random random = new(seed);
        if (islandLayout >= 2) PlanIslands(random); else PlanLegacyIslands(random);
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
            // Layout 3 has no midwater ledges: every ruin rests on the slope or the plain.
            int edge = (int)(Width * (.12 + .60 * ((i * 3) % 8) / 7d));
            int y = i < 4 && islandLayout < 3 ? SeaLevel + (int)(Depth * (.22 + i * .16)) : Floor(edge) - 8;
            Landmarks.Add(new(new(X(edge), y), i % 3 + 1));
        }
    }

    public static int WidthFor(int worldWidth) => Math.Clamp(worldWidth / 6, 200, worldWidth / 5);

    // A sea that reads as deep on the map without walling off the whole
    // underground: 14% of the world height, never below the Underworld envelope.
    internal static int BasinDepth(int worldHeight, int available) =>
        Math.Min(available, Math.Max(360, worldHeight * 14 / 100));

    private void PlanLegacyIslands(Random random)
    {
        int count = Math.Clamp(Width / 650, 3, 7);
        for (int i = 0; i < count; i++)
        {
            int edge = (int)(Width * (.13 + .60 * i / Math.Max(1, count - 1)));
            int radius = Math.Clamp(Width / (count * 13), 24, 90) + random.Next(0, 12);
            bool harbor = count >= 5 ? i is 0 or 2 or 4 : i == count / 2;
            if(harbor)radius=Math.Max(radius,Math.Clamp(Width/30,60,124));
            int crown=random.Next(12,29);
            int keel=harbor?random.Next(85,125):random.Next(90,180)+(i%2)*Depth/20;
            Islands.Add(new(edge,radius,crown,keel,harbor));
        }
    }

    private void PlanIslands(Random random)
    {
        int count = Math.Clamp(Width / 900, 3, 5);
        int pitch = (int)(Width * .60 / Math.Max(1, count - 1));
        for (int i = 0; i < count; i++)
        {
            // Three coastal settlement sites share the six shore residences.
            // Their shallow keels leave the deep swimming volume open; their
            // surface footprint still leaves at least 72% of the sea exposed.
            bool harbor = count >= 5 ? i is 0 or 2 or 4 : i == count / 2;
            int edge = (int)(Width * (.13 + .60 * i / Math.Max(1, count - 1))) + random.Next(-pitch / 4, pitch / 4 + 1);
            int radius = Math.Clamp(Width / (count * 11), 28, 96) + random.Next(0, 24);
            if(harbor)radius=Math.Max(radius,Math.Clamp(Width/30,60,124));
            int crown = random.Next(16, 42);
            int keel = harbor ? random.Next(85, 125) : random.Next(90, 170) + (i % 2) * Depth / 20;
            Islands.Add(new(edge, radius, crown, keel, harbor));
        }
    }
    public int X(int edge) => Left ? edge : WorldWidth - 1 - edge;
    public int Edge(int x) => Left ? x : WorldWidth - 1 - x;
    // Layout 3 sea floor, in tiles below the sea: a coastal shelf, a continental slope
    // and an abyssal plain with low hills and trenches.
    public const int ShelfDepth = 320;
    public const double ShelfFrom = .86, PlainTo = .52;
    // Owner, 29.10: the open sea is a water layer over a cave-riddled rock mass that runs down to
    // the Underworld like the Abyss next door. Floor() is the floor of that open layer (~450 tiles
    // under the sea); CaveOpen() decides which cells of the rock below it are flooded caves.
    public const int OpenDepth = 430, CrustDepth = 36, CaveSeal = 40;
    private int DeepFloor(int edge)
    {
        double t = Math.Clamp((double)edge / Width, 0, 1);
        double open = OpenDepth + Math.Sin(edge * .004 + (Seed & 255)) * 40 + Math.Sin(edge * .0011 + (Seed & 31)) * 30;
        double shelf = Math.Min(ShelfDepth, open * .75), depth;
        if (t >= ShelfFrom) depth = shelf * Math.Pow((1 - t) / (1 - ShelfFrom), 1.3);
        else if (t >= ShelfFrom - .06)
        {
            double s = (ShelfFrom - t) / .06;
            depth = shelf + (open - shelf) * s * s * (3 - 2 * s);
        }
        else depth = open;
        // Broad, gentle swells: wrecks, ruins and gardens need stretches with under 4 tiles of rise.
        double relief = Math.Sin(edge * .019) * 2;
        return Math.Clamp(SeaLevel + (int)(depth + relief), SeaLevel + 4, Bottom - CrustDepth - CaveSeal);
    }

    // Value noise on a hashed lattice, smooth-stepped; stable per world seed.
    private double Lattice(int ix, int iy, int salt)
    {
        unchecked
        {
            uint h = (uint)(ix * 374761393 + iy * 668265263 + (Seed + salt) * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xFFFFFF) / (double)0xFFFFFF;
        }
    }
    private double Noise(double x, double y, int salt)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = x - ix, fy = y - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Lattice(ix, iy, salt), b = Lattice(ix + 1, iy, salt), c = Lattice(ix, iy + 1, salt), d = Lattice(ix + 1, iy + 1, salt);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
    internal double Fbm(double x, double y, int salt) =>
        (Noise(x, y, salt) * .55 + Noise(x * 2.1, y * 2.1, salt + 1) * .3 + Noise(x * 4.3, y * 4.3, salt + 2) * .15);

    /// <summary>Whether a cell of the rock mass under the open floor is a flooded cave. Wide, flat
    /// chambers like the Abyss, a few meandering shafts from the floor down and long galleries
    /// tie them together; a crust under the floor, the map edge, the coast and the last rows above
    /// the Underworld stay solid so the sea cannot drain.</summary>
    public int ShaftCount => Math.Clamp(Width / 1000, 3, 6);
    /// <summary>Edge of the centre line of a meandering shaft from the open floor to the bottom.</summary>
    public double ShaftCentre(int k, int y) =>
        Width * (.06 + .8 * (k + .5) / ShaftCount) + Math.Sin(y * .0045 + k * 1.7 + (Seed & 7)) * 70 + Math.Sin(y * .017 + k) * 18;
    // CaveOpen runs for ~23 million cells at generation and on every zone query in play; the shaft
    // lines depend only on the row and the galleries only on the column, so both are tabulated once.
    private const int GallerySpacing = 650, GalleryFirst = 300;
    private double[] shaftCentres, shaftHalves, galleryCentres, galleryHalves;
    private int galleryCount;
    private void BuildCaveTables()
    {
        int rows = Bottom - SeaLevel, shafts = ShaftCount;
        var centres = new double[shafts * rows]; var halves = new double[shafts * rows];
        for (int k = 0; k < shafts; k++)
        for (int r = 0; r < rows; r++)
        {
            int y = SeaLevel + r;
            centres[k * rows + r] = ShaftCentre(k, y);
            halves[k * rows + r] = 8 + 3 * Math.Sin(y * .05 + k);
        }
        int count = Depth / GallerySpacing + 2;
        var gCentres = new double[Width * count]; var gHalves = new double[Width * count];
        for (int e = 0; e < Width; e++)
        for (int j = 0; j < count; j++)
        {
            gCentres[e * count + j] = GalleryFirst + j * GallerySpacing + Math.Sin(e * .004 + j * 2.1) * 60 + Math.Sin(e * .013 + j) * 14;
            gHalves[e * count + j] = 6 + 2 * Math.Sin(e * .03 + j);
        }
        galleryCount = count; galleryCentres = gCentres; galleryHalves = gHalves;
        shaftHalves = halves; shaftCentres = centres;
    }
    // Owner, 30.10: the cave sea ended in a ruler-straight wall on the land side and a flat floor
    // over the Underworld ("boxes"). Under the crust the land-side edge now wanders up to 340
    // tiles inland of the coast column and the bottom up to 110 rows, both by smooth noise; the
    // host's own terrain stays beyond them, and a solid band keeps the water in.
    public const int LandBlend = 340, BottomBlend = 110, BlendSeal = 14;
    private int[] landLimits, bottomLimits;
    public int LandLimit(int y)
    {
        // Tabulated like the cave tables: CaveOpen asks for it on every cell.
        if (landLimits == null)
        {
            var limits = new int[Math.Max(1, Bottom - SeaLevel + 64)];
            for (int r = 0; r < limits.Length; r++)
            {
                int row = SeaLevel - 64 + r, ramp = Math.Clamp((row - (SeaLevel + ShelfDepth)) / 2, 0, LandBlend);
                limits[r] = Width - (int)(Fbm(row / 150.0, 3.7, 23) * ramp * 1.35);
            }
            landLimits = limits;
        }
        int i = y - (SeaLevel - 64);
        return i < 0 ? Width : landLimits[Math.Min(i, landLimits.Length - 1)];
    }
    public int BottomLimit(int edge)
    {
        if (bottomLimits == null)
        {
            var limits = new int[Math.Max(1, Width)];
            for (int e = 0; e < limits.Length; e++) limits[e] = Bottom - (int)(Fbm(e / 130.0, 8.1, 29) * BottomBlend * 1.35);
            bottomLimits = limits;
        }
        return bottomLimits[Math.Clamp(edge, 0, bottomLimits.Length - 1)];
    }
    /// <summary>Whether the ocean pass writes this cell (layout 3 leaves the host beyond the blended edges).</summary>
    public bool Owns(int edge, int y)
    {
        if (edge < 20 || edge >= Width || y < SeaLevel - 64) return false;
        if (IslandLayout < 3) return y < Floor(edge) + 24;
        if (y >= Bottom) return false;
        if (y < Floor(edge) + CrustDepth) return true;
        return edge < LandLimit(y) && y < BottomLimit(edge);
    }
    public bool CaveOpen(int edge, int y, int floor)
    {
        // Shafts open at the floor row itself; chambers and galleries only under the crust.
        if (IslandLayout < 3 || y < floor || y >= Bottom - CaveSeal || edge < 60 || edge > Width - 100) return false;
        if (y > floor + CrustDepth && (edge >= LandLimit(y) - BlendSeal || y >= BottomLimit(edge) - CaveSeal)) return false;
        if (shaftCentres == null) BuildCaveTables();
        int rows = Bottom - SeaLevel, row = y - SeaLevel;
        for (int k = 0; k < ShaftCount; k++)
        {
            if (Math.Abs(edge - shaftCentres[k * rows + row]) < shaftHalves[k * rows + row]) return true;
        }
        if (y <= floor + CrustDepth) return false;
        int below = y - floor;
        for (int j = 0; j < galleryCount && j * GallerySpacing + GalleryFirst < Bottom - floor; j++)
        {
            if (Math.Abs(below - galleryCentres[edge * galleryCount + j]) < galleryHalves[edge * galleryCount + j]) return true;
        }
        // Chambers thin out under the crust and over the sealed bottom.
        double fade = Math.Min(1, (below - CrustDepth) / 80.0) * Math.Min(1, (Bottom - CaveSeal - y) / 80.0);
        return Fbm(edge / 230.0, y / 95.0, 11) * fade > .555;
    }
    private int[] floorCache;
    private int[] BuildFloorCache()
    {
        var floors = new int[Width];
        for (int e = 0; e < Width; e++) floors[e] = DeepFloor(e);
        return floors;
    }
    public int Floor(int edge)
    {
        if (IslandLayout >= 3)
        {
            if (edge < 0 || edge >= Width) return DeepFloor(edge);
            floorCache ??= BuildFloorCache();
            return floorCache[edge];
        }
        double t = Math.Clamp((double)edge / Width, 0, 1);
        // 65% of the width has near-full depth; the inland 17% is a short shelf.
        double deep = t < .65 ? .97 : t < .83 ? .97 - (t - .65) / .18 * .75 : .22 * Math.Pow((1 - t) / .17, 1.5);
        double relief = Math.Sin(edge * .013 + (Seed & 255)) * 9 + Math.Sin(edge * .037) * 4;
        if (t < .65) relief -= Math.Pow(Math.Max(0, Math.Sin(edge * .004 + (Seed & 15))), 8) * Depth * .06;
        return Math.Clamp(SeaLevel + (int)(Depth * deep + relief), SeaLevel + 4, Bottom);
    }
    public int CliffEdge(int y)
    {
        int low = (int)(Width * (IslandLayout >= 3 ? PlainTo : .65)), high = Width - 1;
        while (high - low > 1) { int mid = (low + high) / 2; if (Floor(mid) > y) low = mid; else high = mid; }
        return low;
    }
    public (int Top,int Bottom) IslandSpan(int edge)
    {
        var column = IslandColumn(edge);
        return (column.Top, column.Bottom);
    }
    // Half-width that a seamount flank adds at a given depth below the sea: a steep
    // stem under the islet that flares into a broad foot on the plain.
    // Natural volcanic-island profile: a shallow reef shelf, a steep but uneven slope with
    // ledges, and a wide talus apron in the last stretch above the floor.
    // O2 (29.10): a broad flank turned five seamounts into one wall of rock and left no open
    // floor, so the stem now widens slowly (1 tile per ~110 of depth) and flares only near the
    // foot: about 44% of the basin is rock instead of ~90% (scratch model of the owner's seed).
    private double FlankReach(OceanIslet i, double depth, double floorDepth)
    {
        double shelf = Math.Min(depth, 30) * .8;
        double slope = Math.Max(0, depth - 30) * .06;
        // The open floor is ~450 deep now: a cone that flares into a talus foot on the floor.
        double apronStart = Math.Max(60, floorDepth - 180);
        double apron = depth > apronStart ? Math.Pow((depth - apronStart) / 180d, 1.8) * 70 : 0;
        double ledges = Math.Sin(depth * .021 + i.Edge * .37) * 6 + Math.Sin(depth * .0047 + i.Edge) * 16;
        return Math.Max(0, shelf + slope + apron + ledges * Math.Min(1, depth / 200));
    }
    private OceanIsletColumn SeamountColumn(int edge)
    {
        var best = new OceanIsletColumn(int.MaxValue, int.MinValue, 1, false);
        int floor = Floor(edge);
        foreach (var i in Islands)
        {
            int dist = Math.Abs(edge - i.Edge);
            double d = dist / (double)i.Radius;
            int top;
            if (d < 1)
                top = SeaLevel - (int)(i.Crown * Math.Pow(1 - d * d, .65)) +
                    (int)Math.Round(Math.Sin(edge * .23 + (Seed & 63)) * 1.5 + Math.Sin(edge * .071 + i.Edge) * 2.5);
            else
            {
                // Solve FlankReach(depth) = dist - radius (monotonic in depth).
                // Shallowest depth whose reach covers the distance (the profile has ledges, so scan).
                double need = dist - i.Radius; int span = floor - SeaLevel, found = -1;
                for (int dd = 0; dd <= span; dd += 4) if (FlankReach(i, dd, span) >= need) { found = dd; break; }
                if (found < 0) continue;
                top = SeaLevel + found + (int)Math.Round(Math.Sin(edge * .052 + i.Edge) * 3 + Math.Sin(edge * .013) * 5);
            }
            if (top >= floor) continue;
            if (top < best.Top) best = new(top, floor + 24, d, i.Harbor);
        }
        return best;
    }
    private OceanIsletColumn[] seamountCache;
    public OceanIsletColumn IslandColumn(int edge)
    {
        if (IslandLayout >= 3)
        {
            // Zone queries run at play time too; the profile is fixed per world, so cache it.
            if (edge < 0 || edge >= Width) return SeamountColumn(edge);
            seamountCache ??= new OceanIsletColumn[Width];
            ref OceanIsletColumn cached = ref seamountCache[edge];
            if (cached.Top == 0 && cached.Bottom == 0) cached = SeamountColumn(edge);
            return cached;
        }
        foreach (var i in Islands)
        {
            double d = Math.Abs(edge-i.Edge)/(double)i.Radius;
            if (d>=1)continue;
            int top = SeaLevel - (int)(i.Crown * Math.Pow(1 - d * d, .65));
            if (IslandLayout >= 2)
                top += (int)Math.Round(Math.Sin(edge * .23 + (Seed & 63)) * 1.5 + Math.Sin(edge * .071 + i.Edge) * 2.5);
            return new(top, SeaLevel + (int)(i.Keel * Math.Pow(1 - d * d, .55)), d, i.Harbor);
        }
        return new(int.MaxValue, int.MinValue, 1, false);
    }
    public int IslandFloor(int edge)
    {
        var column = IslandColumn(edge);
        return column.Top <= column.Bottom ? column.Top : int.MaxValue;
    }
    public bool IslandSolid(int edge, int y)
    {
        var column = IslandColumn(edge);
        return y >= column.Top && y <= column.Bottom;
    }
    public OceanExpeditionZone Zone(int x, int y)
    {
        int edge = Edge(x);
        if (edge < 20 || edge >= Width || y < SeaLevel - 48 || y >= Bottom + 20) return OceanExpeditionZone.None;
        foreach (var room in Grottos)
            if (InsideEllipse(room.Bounds, x, y, 8)) return OceanExpeditionZone.AirGrotto;
        int floor = Floor(edge);
        if (IslandSolid(edge, y) || (y > floor && !CaveOpen(edge, y, floor))) return OceanExpeditionZone.None;
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
