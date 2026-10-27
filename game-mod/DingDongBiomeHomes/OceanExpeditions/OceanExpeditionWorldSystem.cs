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

    internal static void Attach(List<GenPass> tasks, ref double weight)
    {
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
        var plan = new OceanExpeditionLayout(Main.maxTilesX, Main.maxTilesY, left, water, NativeUnderworldSystem.Layer, WorldGen.genRand.Next());
        Apply(plan);
        Layout = plan;
        FishingWaterWorldSystem.RecordExpeditionOcean(plan);
        NativeCoastalBiomeSystem.CaptureGeneratedBounds();
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
