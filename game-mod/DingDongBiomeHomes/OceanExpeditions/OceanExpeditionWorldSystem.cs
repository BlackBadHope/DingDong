using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DingDongBiomeHomes.Fishing;
using DingDongBiomeHomes.Fishing.Worldgen;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.OceanExpeditions;

public sealed partial class OceanExpeditionWorldSystem : ModSystem
{
    public static OceanExpeditionLayout Layout { get; private set; }
    public static bool Active => Layout != null;
    public override void ClearWorld() => Layout = null;
    public override void OnWorldUnload() => Layout = null;
    public override void PostWorldGen() => OceanForeignFootprint.Release(); // V03 29.10

    internal static void Attach(List<GenPass> tasks, ref double weight)
    {
        OceanForeignFootprint.Arm(false);
        if (!ContinentalWorldDimensions.IsContinental || Main.remixWorld || Main.notTheBeesWorld ||
            global::DingDongBiomeHomes.Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive) return;
        // Providers finish their real underwater structures first. Their tiles and
        // reservations become islands of preserved content inside the new basin.
        int lastProvider = tasks.FindLastIndex(p => p.Name is "Abyss" or "Sulphur Sea 2" or "SOTS: Ocean Caves" or "Thorium Mod: Aquatic Depths");
        int settle = tasks.FindIndex(Math.Max(0, lastProvider + 1), p => p.Name.Contains("Settle Liquids"));
        int at = settle >= 0 ? settle : Math.Max(0, lastProvider + 1);
        if (at <= 0) throw new InvalidOperationException("Deep ocean pass has no safe provider boundary.");
        tasks.Insert(at, new PassLegacy("DingDong open ocean", (p, _) =>
        {
            p.Message = "Opening the island sea, deep routes and air grottoes";
            Generate();
        }, 1.6));
        weight += 1.6;
        OceanForeignFootprint.Arm(true);
        // V03 29.10: SOTS Pyramid/AbandonedVillage/Sanctuary/GemStructures run after the sea;
        // the receipt follows the last foreign pass.
        int lastForeign = tasks.FindLastIndex(p => !p.Name.StartsWith("DingDong", StringComparison.Ordinal));
        if (lastForeign > at)
        {
            tasks.Insert(lastForeign + 1, new PassLegacy("DingDong ocean foreign footprint receipt", (p, _) =>
            {
                p.Message = "Checking the island sea for foreign structures";
                OceanForeignFootprint.Receipt(ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod);
            }, 0.01));
            weight += 0.01;
        }
    }
    private static void Generate()
    {
        if (!FishingWaterWorldSystem.Generating) return;
        // In Calamity the sulphur/Abyss side follows the dungeon. Never carve it.
        bool abyssLeft = ModLoader.HasMod("CalamityMod") ? FishingWaterWorldSystem.AbyssLeft : Main.dungeonX < Main.maxTilesX / 2;
        bool left = !abyssLeft;
        var old = FishingWaterWorldSystem.Regions.FirstOrDefault(r => r.Shape == FishingRegionShape.Ocean && (r.Bounds.Center.X < Main.maxTilesX / 2) == left);
        int water = old.Bounds.Width > 0 ? old.WaterY : FindSea(left);
        if (water < 100) throw new InvalidOperationException("Ordinary ocean waterline unavailable.");
        // The recorded waterline predates later terrain passes. A basin filled above
        // the inland rim spills over it when the world first settles, leaving the
        // islets hanging above the real surface; hold the sea at the rim instead.
        int rim = InlandRim(left, OceanExpeditionLayout.WidthFor(Main.maxTilesX));
        int sea = Math.Clamp(Math.Max(water, rim), water, water + 160);
        ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod.Logger.Info($"DINGDONG_OPEN_OCEAN_LEVEL|recordedWaterline={water}|inlandRim={rim}|sea={sea}");
        var plan = new OceanExpeditionLayout(Main.maxTilesX, Main.maxTilesY, left, sea, NativeUnderworldSystem.Layer, WorldGen.genRand.Next());
        Apply(plan);
        Layout = plan;
        OceanForeignFootprint.CaptureBaseline(plan, ModContent.GetInstance<OceanExpeditionWorldSystem>().Mod); // V03 29.10
        FishingWaterWorldSystem.RecordExpeditionOcean(plan);
        NativeCoastalBiomeSystem.CaptureGeneratedBounds();
    }
    // Highest row the water may reach without escaping over the land just inland
    // of the basin: the ground of the lowest rim columns, ignoring isolated
    // crevices (90th percentile over the sampled band).
    private static int InlandRim(bool left, int width)
    {
        var grounds = new List<int>();
        for (int edge = width - 8; edge < width + 160; edge++)
        {
            int x = left ? edge : Main.maxTilesX - 1 - edge;
            if (x < 20 || x >= Main.maxTilesX - 20) continue;
            for (int y = 100; y < Main.maxTilesY - 300; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) { grounds.Add(y); break; }
            }
        }
        if (grounds.Count == 0) return 0;
        grounds.Sort();
        return grounds[Math.Min(grounds.Count - 1, grounds.Count * 9 / 10)];
    }
    private static int FindSea(bool left)
    {
        int x = left ? 100 : Main.maxTilesX - 101;
        for (int y = 100; y < Main.worldSurface + 400 && y < NativeUnderworldSystem.Layer; y++)
            if (FishingEnvironment.WaterAt(x, y) && FishingEnvironment.WaterAt(x, y + 4)) return y;
        return -1;
    }
    public static bool TryGetZone(int x, int y, out OceanExpeditionZone zone)
    {
        zone = Layout?.Zone(x, y) ?? OceanExpeditionZone.None;
        if (zone == OceanExpeditionZone.AirGrotto && WorldGen.InWorld(x, y) && Main.tile[x, y].LiquidAmount >= 128)
            zone = OceanExpeditionZone.DeepSea;
        return zone != OceanExpeditionZone.None;
    }
    public override void SaveWorldData(TagCompound tag)
    {
        if (Layout == null) return;
        tag["OpenOceanExpeditionV1"] = Encode(Layout);
    }
    private static TagCompound Encode(OceanExpeditionLayout p) => new()
    {
        ["version"] = OceanExpeditionLayout.Version, ["left"] = p.Left, ["sea"] = p.SeaLevel,
        ["underworld"] = p.Bottom + p.Clearance, ["seed"] = p.Seed,
        ["bottom"] = p.Bottom, ["islandLayout"] = p.IslandLayout,
        ["grottos"] = p.Grottos.Select(g => new TagCompound { ["x"] = g.Bounds.X, ["y"] = g.Bounds.Y, ["w"] = g.Bounds.Width, ["h"] = g.Bounds.Height }).ToList(),
        ["landmarks"] = p.Landmarks.Select(g => new TagCompound { ["x"] = g.Floor.X, ["y"] = g.Floor.Y, ["kind"] = g.Kind }).ToList()
    };
    private static OceanExpeditionLayout Decode(TagCompound tag)
    {
        if (tag.GetInt("version") != OceanExpeditionLayout.Version) throw new InvalidDataException("Unknown open ocean layout version.");
        // Worlds saved before the shallower basin carry only "underworld": their
        // bottom was the Underworld envelope and their islets used the legacy plan.
        int underworld = tag.GetInt("underworld");
        int bottom = tag.ContainsKey("bottom") ? tag.GetInt("bottom") : underworld - 100;
        int islandLayout = tag.ContainsKey("islandLayout") ? tag.GetInt("islandLayout") : 1;
        var p = new OceanExpeditionLayout(Main.maxTilesX, Main.maxTilesY, tag.GetBool("left"), tag.GetInt("sea"), underworld, tag.GetInt("seed"), bottom, islandLayout);
        var rooms = tag.GetList<TagCompound>("grottos"); var sites = tag.GetList<TagCompound>("landmarks");
        if (rooms.Count > 8 || sites.Count > 32) throw new InvalidDataException("Open ocean POI count exceeds bounds.");
        p.Grottos.Clear(); p.Landmarks.Clear();
        foreach (var r in rooms)
        {
            Rectangle b = new(r.GetInt("x"), r.GetInt("y"), r.GetInt("w"), r.GetInt("h"));
            if (b.Width is < 40 or > 400 || b.Height is < 24 or > 200 || !p.Bounds.Contains(b)) throw new InvalidDataException("Invalid ocean grotto.");
            p.Grottos.Add(new(b));
        }
        foreach (var r in sites)
        {
            Point point = new(r.GetInt("x"), r.GetInt("y")); int kind = r.GetInt("kind");
            if (!p.Bounds.Contains(point) || kind is < 0 or > 3) throw new InvalidDataException("Invalid ocean landmark.");
            p.Landmarks.Add(new(point, kind));
        }
        return p;
    }
    public override void LoadWorldData(TagCompound tag)
    {
        Layout = tag.ContainsKey("OpenOceanExpeditionV1") ? Decode(tag.GetCompound("OpenOceanExpeditionV1")) : null;
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Active);
        if (Active) TagIO.Write(Encode(Layout), writer);
    }
    public override void NetReceive(BinaryReader reader) => Layout = reader.ReadBoolean() ? Decode(TagIO.Read(reader)) : null;
}
