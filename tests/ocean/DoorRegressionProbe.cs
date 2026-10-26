using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
    internal static object ProbeDoorClearance(Mod mod)
    {
        var checks = new List<string>();
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks.Add(name); }
        Main.maxTilesX = Main.maxTilesY = 600;
        Main.tile = (Tilemap)Activator.CreateInstance(typeof(Tilemap), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { (ushort)601, (ushort)601 }, null);
        Main.netMode = NetmodeID.Server; WorldGen.gen = false;
        foreach (int scenario in new[] { -1, 0, 1, 2, 3 })
        {
            Point origin = new(80 + (scenario + 1) * 80, 100);
            int x = origin.X + 5, top = origin.Y + 3;
            ushort type = scenario is 1 or 2 ? TileID.OpenDoor : TileID.ClosedDoor;
            int width = type == TileID.OpenDoor ? 2 : 1;
            void SetDoor(int xx, int yy, ushort door, int orientation)
            {
                int w = door == TileID.OpenDoor ? 2 : 1;
                for (int dx = 0; dx < w; dx++) for (int dy = 0; dy < 3; dy++)
                { Tile t = Main.tile[xx + dx, yy + dy]; t.HasTile = true; t.TileType = door; t.TileFrameX = (short)(orientation + dx * 18); t.TileFrameY = (short)(54 * 2 + dy * 18); t.WallType = WallID.GrayBrick; t.RedWire = true; }
                foreach (int y in new[] { yy - 1, yy + 3 }) { Tile t = Main.tile[xx, y]; t.HasTile = true; t.TileType = TileID.Stone; }
            }
            SetDoor(x, top, type, scenario == 2 ? 36 : 0);
            SetDoor(x + 3, top, TileID.ClosedDoor, 0);
            bool[] writes = new bool[100]; writes[5 * 10 + 5] = true;
            if (scenario == 3) for (int dy = 3; dy <= 5; dy++) writes[5 * 10 + dy] = true;
            var plan = new StructureWritePlan(writes, new bool[100], writes, scenario == 3 ? 3 : 1, new(5, 3, 1, 3), null, null, 0);
            if (scenario >= 0)
            {
                ClearIntersectedDoors(mod, plan, 10, 10, origin);
                bool cleared = true;
                for (int dx = 0; dx < width; dx++) for (int dy = 0; dy < 3; dy++) cleared &= !Main.tile[x + dx, top + dy].HasTile;
                Check(cleared, $"{scenario}: complete intersected old door removed before paste");
                Check(Main.tile[x, top].WallType == WallID.GrayBrick && Main.tile[x, top].RedWire, $"{scenario}: old door walls and wires retained");
            }
            if (scenario == 3) SetDoor(x, top, TileID.ClosedDoor, 0);
            else { Tile t = Main.tile[x, top + 2]; t.ClearEverything(); t.HasTile = true; t.TileType = TileID.Cloud; }
            AuthoredSettlementBuilder.FrameAuthoredMutation(new Rectangle(x, top, 1, 3));
            if (scenario == -1) Check(!Main.tile[x, top + 2].HasTile, "Native legacy-sequence control destroys replacement masonry beneath a partial old door");
            else if (scenario == 3) Check(Main.tile[x, top].HasTile && Main.tile[x, top].TileType == TileID.ClosedDoor && Main.tile[x, top + 2].TileType == TileID.ClosedDoor, "New authored door survives framing after old object clearance");
            else Check(Main.tile[x, top + 2].HasTile && Main.tile[x, top + 2].TileType == TileID.Cloud, $"{scenario}: pasted masonry survives real native door framing");
            bool untouched = true; for (int dy = 0; dy < 3; dy++) untouched &= Main.tile[x + 3, top + dy].HasTile && Main.tile[x + 3, top + dy].TileType == TileID.ClosedDoor;
            Check(untouched, $"{scenario}: nearby door outside written cells remains intact");
        }
        WorldGen.gen=true;
        foreach(ushort type in new[]{TileID.LargePiles,TileID.LargePiles2})
        {
            Point origin=new(495,200+(type-TileID.LargePiles)*60);int x=origin.X+5,y=origin.Y+3;
            void Pile()
            {
                for(int dx=0;dx<3;dx++)for(int dy=0;dy<2;dy++){Tile t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;t.TileFrameX=(short)(54*14+dx*18);t.TileFrameY=(short)(dy*18);t.WallType=WallID.GrayBrick;t.RedWire=true;}
            }
            void Paste(){for(int dx=0;dx<3;dx++){Tile t=Main.tile[x+dx,y+1];t.ClearEverything();t.HasTile=true;t.TileType=TileID.Pearlwood;}}
            Pile();Paste();AuthoredSettlementBuilder.FrameAuthoredMutation(new Rectangle(x,y,3,2));
            if(type==TileID.LargePiles2) Check(!Main.tile[x,y+1].HasTile,$"{type}: native legacy rubble control destroys replacement wood");
            Pile();bool[] writes=new bool[100];for(int dx=5;dx<=7;dx++)writes[dx*10+4]=true;
            var plan=new StructureWritePlan(writes,new bool[100],writes,3,new(5,4,3,1),null,null,0);
            ClearIntersectedRubble(mod,plan,10,10,origin);
            bool clear=true;for(int dx=0;dx<3;dx++)for(int dy=0;dy<2;dy++)clear&=!Main.tile[x+dx,y+dy].HasTile;
            Check(clear,$"{type}: complete old rubble footprint cleared before paste");
            Check(Main.tile[x,y].WallType==WallID.GrayBrick&&Main.tile[x,y].RedWire,$"{type}: ambient wall and wire state retained");
            Paste();AuthoredSettlementBuilder.FrameAuthoredMutation(new Rectangle(x,y,3,2));
            bool wood=true;for(int dx=0;dx<3;dx++)wood&=Main.tile[x+dx,y+1].HasTile&&Main.tile[x+dx,y+1].TileType==TileID.Pearlwood;
            Check(wood,$"{type}: all three imported wood blocks survive actual native framing");
        }
        return new { status = "PASS", checks, userSavesUsed = false, nativeFraming = true };
    }
}

public sealed class DoorRegressionProbe : ModSystem
{
    public override void PostAddRecipes()
    {
        string output = Environment.GetEnvironmentVariable("DINGDONG_R38_DOOR_REGRESSION");
        if (output == null) return;
        if (!Main.dedServ || !Path.GetFullPath(output).Contains(@"\Fix-R38\", StringComparison.OrdinalIgnoreCase)) throw new Exception("Disposable fixture required");
        try { File.WriteAllText(output, JsonSerializer.Serialize(DingDongAuthoredStructureLoader.ProbeDoorClearance(Mod), new JsonSerializerOptions { WriteIndented = true })); Environment.Exit(0); }
        catch (Exception ex) { File.WriteAllText(output, JsonSerializer.Serialize(new { status = "FAIL", error = ex.ToString() })); Environment.Exit(1); }
    }
}
