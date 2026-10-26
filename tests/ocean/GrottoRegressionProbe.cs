using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DingDongBiomeHomes.OceanExpeditions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Diagnostics;

public sealed class GrottoRegressionProbe : ModSystem
{
    public override void PostAddRecipes()
    {
        string output = Environment.GetEnvironmentVariable("DINGDONG_R38_GROTTO_PROBE");
        if (output == null) return;
        if (!Main.dedServ || !Path.GetFullPath(output).Contains(@"\Fix-R38\", StringComparison.OrdinalIgnoreCase)) throw new Exception("Disposable fixture required.");
        bool baseline = Environment.GetEnvironmentVariable("DINGDONG_R38_BASELINE") == "1";
        var checks = new List<string>(); var cases = new List<object>(); string error = null;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
        try
        {
            Main.netMode = NetmodeID.Server; Main.myPlayer = 0; WorldGen.gen = true;
            Liquid.curMaxLiquid = Liquid.maxLiquid;
            for (int i = 0; i < Main.liquid.Length; i++) Main.liquid[i] ??= new Liquid();
            for (int i = 0; i < Main.liquidBuffer.Length; i++) Main.liquidBuffer[i] ??= new LiquidBuffer();
            foreach (bool left in new[] { true, false })
            {
                Main.maxTilesX = 8400; Main.maxTilesY = 2400;
                Main.tile = (Tilemap)Activator.CreateInstance(typeof(Tilemap), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { (ushort)8401, (ushort)2401 }, null);
                Main.chest = new Chest[Main.maxChests]; GenVars.structures = new StructureMap();
                Main.worldSurface = 420; Main.rockLayer = 1000;
                Liquid.numLiquid = LiquidBuffer.numLiquidBuffer = 0;
                var p = new OceanExpeditionLayout(8400, 2400, left, 420, 2200, 965786833);
                for (int edge = 20; edge < p.Width + 128; edge++)
                for (int y = p.SeaLevel; y < 2400; y++)
                { Tile t = Main.tile[p.X(edge), y]; t.HasTile = true; t.TileType = TileID.Stone; }
                // A dense provider cliff with one real unreserved gap. The old
                // narrow candidate loop can produce at most one breathing stop.
                var protectedAreas = new List<Rectangle>();
                foreach (var span in new[] { (p.SeaLevel - 64, 980), (1220, p.Bottom + 48) })
                {
                    int x1 = p.X(840), x2 = p.X(p.Width + 32);
                    Rectangle b = new(Math.Min(x1, x2), span.Item1, Math.Abs(x1 - x2) + 1, span.Item2 - span.Item1);
                    protectedAreas.Add(b); GenVars.structures.AddProtectedStructure(b, 8);
                    for (int x = b.Left; x < b.Right; x++)
                    for (int y = b.Top; y < b.Bottom; y++)
                    { Tile t = Main.tile[x, y]; t.HasTile = true; t.TileType = TileID.BlueDungeonBrick; t.WallType = WallID.BlueDungeonUnsafe; }
                }
                // A separate important object must also survive the expanded search.
                int objectX = p.X(650); Main.tile[objectX, 1600].TileType = TileID.Containers;
                Exception refusal = null;
                try { OceanExpeditionWorldSystem.Apply(p); } catch (InvalidOperationException ex) { refusal = ex; }
                cases.Add(new { left, grottos = p.Grottos.Count, requested = 3, chests = Main.chest.Count(c => c != null), refusal = refusal?.Message });
                if (baseline)
                {
                    Check(refusal != null && p.Grottos.Count < 2, $"{left}: released narrow search reproduces content-coverage refusal");
                    continue;
                }
                Check(refusal == null && p.Grottos.Count == 3, $"{left}: all requested grottoes placed despite protected cliff");
                Check(Main.tile[objectX, 1600].TileType == TileID.Containers, $"{left}: important object retained");
                foreach (var area in protectedAreas)
                {
                    bool intact = true;
                    for (int x = area.Left; x < area.Right; x++)
                    for (int y = area.Top; y < area.Bottom; y++)
                    { Tile t = Main.tile[x, y]; intact &= t.HasTile && t.TileType == TileID.BlueDungeonBrick && t.WallType == WallID.BlueDungeonUnsafe; }
                    Check(intact, $"{left}: complete foreign protected tile and wall rectangle retained");
                }
                foreach (var room in p.Grottos)
                {
                    Point air = new(room.Bounds.Center.X, room.Bounds.Center.Y - 5);
                    Check(!Main.tile[air.X, air.Y].HasTile && Main.tile[air.X, air.Y].LiquidAmount == 0, $"{left}: accepted grotto is dry and traversable");
                    int floor = room.Bounds.Center.Y + room.Bounds.Height / 4;
                    int barrier = left ? room.Bounds.Left + 8 : room.Bounds.Right - 11;
                    Check(Main.tile[barrier, floor - 6].TileType == TileID.Bubble, $"{left}: mirrored bubble airlock exists");
                    int exterior = left ? barrier - 1 : barrier + 3;
                    Tile wave = Main.tile[exterior, floor - 6]; wave.LiquidAmount = 64; Liquid.AddWater(exterior, floor - 6);
                    Check(Main.chest.Any(c => c != null && room.Bounds.Contains(c.x, c.y)), $"{left}: finite cache was placed inside grotto");
                }
                Check(Liquid.numLiquid > 0, $"{left}: native water-update queue exercised");
                for (int tick = 0; tick < 600 && (Liquid.numLiquid > 0 || LiquidBuffer.numLiquidBuffer > 0); tick++) Liquid.UpdateLiquid();
                Check(p.Grottos.All(g => Main.tile[g.Bounds.Center.X, g.Bounds.Center.Y - 5].LiquidAmount == 0), $"{left}: all grottoes remain dry after native water update");
                Check(p.Grottos.Any(g => p.Edge(g.Bounds.Center.X) < p.CliffEdge(g.Bounds.Center.Y) - g.Bounds.Width / 2), $"{left}: fallback actually used a seaward reef");
                ModContent.GetInstance<OceanExpeditionWorldSystem>().ClearWorld();
                GC.Collect(); GC.WaitForPendingFinalizers();
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        File.WriteAllText(output, JsonSerializer.Serialize(new { status = error == null ? "PASS" : "FAIL", baseline, checks, cases, error, userSavesWritten = false }, new JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(error == null ? 0 : 1);
    }
}
