using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen;

// Persist an absolute placement, not another getter-derived height. Legacy
// worlds keep marker zero; their physical roof supplies a transient runtime C.
internal sealed class NativeSanctuaryPlacementSystem : ModSystem
{
    private const string Tag = "nativeSanctuaryPlacementCeiling";
    private const string HelperName = "SOTS.WorldgenHelpers.SanctuaryWorldgenHelper";
    private const int NativeSpan = 197, BiomeInset = 80;
    private static int persistedCeiling, resolvedCeiling;
    private static FieldInfo anchorField, rectangleField;
    private static MethodInfo setRectangle;
    private static Mod provider;
    private static bool preflightReported, newGeneration;

    internal static bool Raised => persistedCeiling > 0;
    internal static int PersistedCeiling => persistedCeiling;
    internal static int ResolvedCeiling => resolvedCeiling;
    internal static bool FreshGeneration => newGeneration && NativeTerrariaWorldgenReceipt.InProgress &&
        ContinentalWorldDimensions.IsContinental && NativeUnderworldSystem.Active &&
        !Compiler.Runtime.WorldCompilerActivation.IsActive;

    internal static int AdjustCeiling(int original)
    {
        if (!ContinentalWorldDimensions.IsContinental) return original;
        InitializeGenerationPlacement();
        return resolvedCeiling > 0 ? resolvedCeiling : original;
    }

    internal static int AdjustUnderworldHeight(int original)
    {
        if (!ContinentalWorldDimensions.IsContinental) return original;
        InitializeGenerationPlacement();
        return resolvedCeiling > 0 ? checked(resolvedCeiling + 65) : original;
    }

    internal static int BoundBottom(int original)
    {
        if (!ContinentalWorldDimensions.IsContinental) return original;
        InitializeGenerationPlacement();
        return Raised ? checked(persistedCeiling + NativeSpan) : Math.Min(original, Main.maxTilesY - 3);
    }

    private static void InitializeGenerationPlacement()
    {
        if (!newGeneration || !NativeTerrariaWorldgenReceipt.InProgress || !ContinentalWorldDimensions.IsContinental ||
            !NativeUnderworldSystem.Active) return;
        if (resolvedCeiling == 0)
        {
            int selected = checked(NativeUnderworldSystem.Layer + BiomeInset);
            ValidateCeiling(selected);
            persistedCeiling = resolvedCeiling = selected;
        }
        // Publish C before reflection: a provider type initializer can itself
        // call SetRect and re-enter the adjusted getters. Retry only the cheap
        // preflight until the provider has assigned a valid anchor.
        ReportPreflight();
    }

    public override void ClearWorld()
    {
        persistedCeiling = resolvedCeiling = 0;
        preflightReported = newGeneration = false;
    }

    public override void PreWorldGen()
    {
        ClearWorld();
        newGeneration = true;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        // Do not migrate old worlds merely because their roof was observed.
        if (persistedCeiling > 0) tag[Tag] = persistedCeiling;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        persistedCeiling = tag.GetInt(Tag);
        if (persistedCeiling != 0) ValidateCeiling(persistedCeiling);
        resolvedCeiling = persistedCeiling;
        preflightReported = newGeneration = false;
        // SOTS' SpawnPos and NativeUnderworld may load later in the system list.
        // Defer rectangle refresh until all LoadWorldData callbacks have run.
    }

    public override void PostWorldLoad()
    {
        newGeneration = false;
        if (!ContinentalWorldDimensions.IsContinental || !Bind()) return;
        if (!Raised && TryFindPhysicalRoof(out int roof, out _))
            resolvedCeiling = checked(roof + 15);
        if (resolvedCeiling > 0) RefreshRectangle();
        else Mod.Logger.Warn("DINGDONG_SANCTUARY_PLACEMENT|state=LEGACY_UNRESOLVED|marker=0|worldTilesChanged=False");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Raised);
        writer.Write(resolvedCeiling);
    }

    public override void NetReceive(BinaryReader reader)
    {
        bool raised = reader.ReadBoolean();
        int resolved = reader.ReadInt32();
        if (resolved != 0) ValidateCeiling(resolved);
        if (raised && resolved == 0) throw new InvalidDataException("Raised Sanctuary has no absolute ceiling.");
        persistedCeiling = raised ? resolved : 0;
        resolvedCeiling = resolved;
        newGeneration = false;
        if (ContinentalWorldDimensions.IsContinental && resolvedCeiling > 0 && Bind()) RefreshRectangle();
    }

    private static void ValidateCeiling(int ceiling)
    {
        // No NativeUnderworld dependency here: persisted/networked C must be
        // usable before that system's LoadWorldData or NetReceive callback.
        if (ceiling < 60 || ceiling > Main.maxTilesY - NativeSpan - 3)
            throw new InvalidDataException("Sanctuary absolute ceiling is outside the world: " + ceiling);
    }

    private static bool Bind()
    {
        if (provider != null) return true;
        if (!ModLoader.TryGetMod("SOTS", out Mod sots)) return false;
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = sots.Code.GetType(HelperName, true);
        FieldInfo anchor = type.GetField("SpawnPos", flags), rectangle = type.GetField("Rectangle", flags);
        MethodInfo setter = type.GetMethod("SetRect", flags, null, Type.EmptyTypes, null);
        if (anchor?.FieldType != typeof(int) || rectangle?.FieldType != typeof(Rectangle) ||
            rectangle.IsInitOnly || setter?.ReturnType != typeof(Rectangle))
            throw new MissingMemberException("Unsupported SOTS Sanctuary placement metadata ABI.");
        anchorField = anchor;
        rectangleField = rectangle;
        setRectangle = setter;
        provider = sots;
        return true;
    }

    private static void RefreshRectangle()
    {
        // Invoke the provider's exact rectangle algorithm after its audited C/B
        // callsites have been adjusted. No tiles, SpawnPos or world files change.
        Rectangle value = (Rectangle)setRectangle.Invoke(null, null);
        rectangleField.SetValue(null, value);
    }

    internal static bool TryFindPhysicalRoof(out int roof, out int anchor)
    {
        roof = -1; anchor = -1;
        if (!ContinentalWorldDimensions.IsContinental || !Bind()) return false;
        anchor = (int)anchorField.GetValue(null);
        if (anchor < 420 || anchor + 420 >= Main.maxTilesX) return false;
        HashSet<ushort> tiles = new();
        foreach (string name in new[] { "EvostoneBrickTile", "RunicEvostoneBrickTile", "EvostoneTile",
            "RunicEvostoneTile", "OvergrownEvostoneTile", "OvergrownEvostoneBrickTile", "InvidiaPlatingTile" })
            if (provider.TryFind<ModTile>(name, out var tile)) tiles.Add((ushort)tile.Type);
        // A roof below this point cannot leave the audited 197-tile structure
        // inside the world. Do not mistake a damaged legacy roof's lower floor
        // for another ceiling and turn it into an invalid network coordinate.
        int lastRoof = Main.maxTilesY - NativeSpan - 3 - 15;
        for (int y = Main.maxTilesY - 700; y <= lastRoof; y++)
        {
            int count = 0;
            for (int x = anchor - 400; x <= anchor + 400; x++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile && tiles.Contains(tile.TileType)) count++;
            }
            if (count >= 801 * 3 / 4) { roof = y; return true; }
        }
        return false;
    }

    private static void ReportPreflight()
    {
        if (preflightReported || !Bind()) return;
        int anchor = (int)anchorField.GetValue(null);
        if (anchor < 510 || anchor + 510 >= Main.maxTilesX) return;
        preflightReported = true;
        Rectangle envelope = new(anchor - 510, resolvedCeiling - 50, 1021, NativeSpan + 61);
        int containers = 0, reservations = 0;
        var containerLocations = new List<object>();
        foreach (Chest chest in Main.chest)
        {
            if (chest == null) continue;
            Rectangle halo = new(chest.x - 4, chest.y - 4, 10, 10);
            if (!envelope.Intersects(halo)) continue;
            containers++;
            bool inWorld = chest.x >= 0 && chest.y >= 0 && chest.x < Main.maxTilesX && chest.y < Main.maxTilesY;
            containerLocations.Add(new { x = chest.x, y = chest.y,
                tileType = inWorld ? (int)Main.tile[chest.x, chest.y].TileType : -1,
                hasTile = inWorld && Main.tile[chest.x, chest.y].HasTile });
        }
        try
        {
            foreach (Rectangle region in ForeignStructureIntegrityGuard.ReadReservations())
                if (envelope.Intersects(region)) reservations++;
        }
        catch (Exception error)
        {
            provider.Logger.Warn("DINGDONG_SANCTUARY_PREFLIGHT|reservations=UNKNOWN|" + error.GetType().Name);
        }
        if (containers > 0)
        {
            provider.Logger.Warn("DINGDONG_SANCTUARY_PREFLIGHT|containerLocations=" + JsonSerializer.Serialize(containerLocations));
            // Admission must prevent this collision before a native builder
            // writes tiles; no chest relocation or item deletion is allowed.
            throw new InvalidOperationException("Raised SOTS Sanctuary still intersects a container despite early placement admission; generation rejected.");
        }
        provider.Logger.Info($"DINGDONG_SANCTUARY_PREFLIGHT|anchor={anchor}|ceiling={resolvedCeiling}" +
            $"|bottom={resolvedCeiling + NativeSpan}|containerHaloIntersections={containers}" +
            $"|reservationEnvelopeIntersections={reservations}|containerPreparationRequested={containers > 0}" +
            "|envelopeIsConservative=True");
    }

    internal static bool ValidateGeometry(Mod owner)
    {
        if (!ContinentalWorldDimensions.IsContinental || !Bind()) return true;
        bool found = TryFindPhysicalRoof(out int roof, out int anchor);
        int expected = resolvedCeiling > 0 ? resolvedCeiling - 15 : -1;
        bool valid = found && expected >= 0 && roof == expected;
        int bottom = Raised ? persistedCeiling + NativeSpan : Main.maxTilesY - 3;
        owner.Logger.Info($"DINGDONG_SANCTUARY_PLACEMENT|verdict={(valid ? "PASS" : "FAIL")}|raised={Raised}" +
            $"|persistedC={persistedCeiling}|resolvedC={resolvedCeiling}|physicalRoof={roof}" +
            $"|expectedRoof={expected}|bottom={bottom}|worldTilesChanged=False");
        if (found && (Environment.GetEnvironmentVariable("DINGDONG_SANCTUARY_PLACEMENT_PROBE") == "1" ||
            Environment.GetEnvironmentVariable("DINGDONG_BIG_GENERATION_PROBE") == "1"))
        {
            string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "SanctuaryPlacement",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            Directory.CreateDirectory(directory);
            MapHelper.Initialize();
            NativeTerrainRaster.ExportRegion(Path.Combine(directory, "context.bmp"),
                new(anchor - 550, Math.Max(0, roof - 100), 1101, Math.Min(400, Main.maxTilesY - roof + 100)));
            File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new {
                valid, raised = Raised, persistedCeiling, resolvedCeiling, anchor,
                physicalRoofY = roof, expectedRoofY = expected, bottom, worldTilesChanged = false
            }, new JsonSerializerOptions { WriteIndented = true }));
            owner.Logger.Info("DINGDONG_SANCTUARY_PLACEMENT|directory=" + directory);
        }
        return valid;
    }

    public override void Unload()
    {
        ClearWorld();
        anchorField = rectangleField = null;
        setRectangle = null;
        provider = null;
    }
}
