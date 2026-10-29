using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
namespace DingDongBiomeHomes;

internal static class ResidentEncounterTerrain
{
    private static readonly Dictionary<string,Point> HomeAnchors=new(StringComparer.Ordinal);
    internal static void Reset()=>HomeAnchors.Clear();
    // Anchor point uses the same standing-cell convention as authored quest points:
    // support is two tiles below it. Clearance is checked for the actual NPC size.
    internal static bool TryCenter(Point point,int type,out Vector2 center)
    {
        center=default;if(point.X<3||point.Y<5||point.X>=Main.maxTilesX-3||point.Y>=Main.maxTilesY-4||!ContentSamples.NpcsByNetId.TryGetValue(type,out var sample))return false;
        int floor=point.Y+2;
        center=new Vector2(point.X*16+8,Math.Min(point.Y*16+8,floor*16-sample.height*.5f));
        Vector2 top=center-new Vector2(sample.width*.5f,sample.height*.5f);
        int left=(int)top.X/16,right=(int)(top.X+sample.width-1)/16;
        if(left<2||right>=Main.maxTilesX-2||top.Y<32)return false;
        bool support=false;
        for(int x=left;x<=right;x++)support|=Supports(x,floor);
        if(!support)return false;
        if(Collision.SolidCollision(top,sample.width,sample.height))return false;
        for(int x=(int)top.X/16;x<=(int)(top.X+sample.width-1)/16;x++)for(int y=(int)top.Y/16;y<=floor;y++)
            if(Main.tile[x,y].LiquidAmount>0&&Main.tile[x,y].LiquidType==LiquidID.Lava)return false;
        return true;
    }
    private static bool Supports(int x,int y)
    {var tile=Main.tile[x,y];return tile.HasTile&&!tile.IsActuated&&(Main.tileSolid[tile.TileType]||Main.tileSolidTop[tile.TileType]);}
    internal static bool TryHomeAnchor(SettlementResidenceReport home,int type,out Point point)
    {
        if(HomeAnchors.TryGetValue(home.NpcKey,out point)&&Valid(point))return true;
        for(int i=0;i<16;i++)
        {
            var candidate=home.QuestPoint(i>=8,i%8);
            if(Valid(candidate)){point=candidate;HomeAnchors[home.NpcKey]=point;return true;}
        }
        // Large residents need their courtyard or roof instead of a player-size
        // room. Search only the authored residence and its entrance margin.
        var search=home.Area;search.Inflate(8,12);
        for(int y=Math.Max(6,search.Top);y<Math.Min(Main.maxTilesY-5,search.Bottom);y++)
        for(int x=Math.Max(6,search.Left);x<Math.Min(Main.maxTilesX-5,search.Right);x++)
        {var candidate=new Point(x,y);if(!Valid(candidate))continue;point=candidate;HomeAnchors[home.NpcKey]=point;return true;}
        point=Point.Zero;return false;
        bool Valid(Point candidate)=>WorldGen.InWorld(candidate.X,candidate.Y,5)&&Supports(candidate.X,candidate.Y+2)&&TryCenter(candidate,type,out _);
    }
    internal static bool TryAssignedHomeAnchor(Point nativeHome,int type,out Point anchor)
    {
        anchor=Point.Zero;
        if(!WorldGen.InWorld(nativeHome.X,nativeHome.Y,8))return false;
        // Vanilla home coordinates identify a housing position, not necessarily
        // its floor. Find support below that position within one small room.
        var exact=new Point(nativeHome.X,nativeHome.Y-2);
        if(TryCenter(exact,type,out _)){anchor=exact;return true;}
        for(int radius=0;radius<=6;radius++)
        for(int side=0;side<(radius==0?1:2);side++)
        {
            int x=nativeHome.X+(side==0?radius:-radius);
            for(int floor=Math.Max(8,nativeHome.Y-4);floor<=Math.Min(Main.maxTilesY-8,nativeHome.Y+32);floor++)
            {
                var point=new Point(x,floor-2);
                if(!TryCenter(point,type,out _))continue;
                anchor=point;return true;
            }
        }
        return false;
    }
    internal static bool TryAnchor(string key,SettlementResidenceReport home,bool night,out Point anchor)
    {
        anchor=Point.Zero;int type=ResidentEncounterSystem.PendingType(key);int cx=home.Area.Center.X;
        bool dungeon=key is "Terraria/Clothier" or "Terraria/Mechanic" or "Terraria/Steampunker";
        if(dungeon)cx=Main.dungeonX;
        int top,bottom;
        bool surface=key is "Terraria/Angler" or "Terraria/DD2Bartender" or "Terraria/SantaClaus"||key=="Terraria/Dryad"&&!night;
        if(key=="Terraria/TaxCollector"){top=Main.UnderworldLayer+10;bottom=Main.maxTilesY-40;}
        else if(dungeon){top=key=="Terraria/Clothier"?Math.Max(20,Main.dungeonY-60):Main.dungeonY+25;bottom=key=="Terraria/Clothier"?Main.dungeonY+100:Math.Min(Main.UnderworldLayer-100,Main.dungeonY+1800);}
        else if(surface){top=Math.Max(20,home.Area.Top-500);bottom=Math.Min(Main.UnderworldLayer-100,home.Area.Bottom+500);}
        else {top=Math.Max(40,home.Area.Center.Y+120);bottom=Main.UnderworldLayer-100;}
        if(key=="Terraria/Stylist")
        {
            // Spider nests can be far from the stylist's coastal residence.
            // One strided survey is bounded by world dimensions and cached in world data.
            top=Math.Max(80,(int)Main.worldSurface);bottom=Main.UnderworldLayer-80;
            for(int y=top;y<bottom;y+=16)for(int x=80;x<Main.maxTilesX-80;x+=16)
                if(Main.tile[x,y].WallType==WallID.SpiderUnsafe&&Search(x-48,x+48,y-48,y+64,out anchor))return true;
            return false;
        }
        // Search nearest columns first; keep wanderers outside their residence.
        for(int radius=48;radius<=1400;radius+=32)
            foreach(int sign in new[]{-1,1})
            {
                int x=cx+radius*sign;
                if(Search(x-8,x+8,top,bottom,out anchor))return true;
            }
        return false;

        bool Search(int left,int right,int first,int last,out Point found)
        {
            found=Point.Zero;
            for(int x=Math.Max(8,left);x<=Math.Min(Main.maxTilesX-8,right);x+=4)
            for(int y=Math.Max(8,first);y<=Math.Min(Main.maxTilesY-8,last);y++)
            {
                var floor=Main.tile[x,y];if(!floor.HasTile||floor.IsActuated||!Main.tileSolid[floor.TileType])continue;
                if(home.Area.Contains(x,y))continue;
                var above=Main.tile[x,y-2];
                if(dungeon&&key!="Terraria/Clothier"&&!Main.wallDungeon[above.WallType])continue;
                if(key=="Terraria/Stylist"&&above.WallType!=WallID.SpiderUnsafe)continue;
                if(key=="Terraria/Dryad"&&floor.TileType is not (TileID.JungleGrass or TileID.Mud))continue;
                if(key=="Terraria/Golfer"&&floor.TileType is not (TileID.Sand or TileID.HardenedSand or TileID.Sandstone))continue;
                if(key=="Terraria/SantaClaus"&&floor.TileType is not (TileID.SnowBlock or TileID.IceBlock))continue;
                if(key=="Terraria/Angler"&&floor.TileType!=TileID.Sand)continue;
                if(surface && (above.WallType!=0||Main.tile[x,Math.Max(1,y-24)].HasTile))continue;
                if(!surface&&!dungeon&&key!="Terraria/TaxCollector"&&above.WallType==0&&y<Main.rockLayer)continue;
                var candidate=new Point(x,y-2);if(!TryCenter(candidate,type,out _))continue;
                // Land rescues never begin submerged. Ocean sleeping angler stays on an island/shore.
                if(above.LiquidAmount>0||Main.tile[x,y-1].LiquidAmount>0)continue;
                found=candidate;return true;
            }
            return false;
        }
    }
}
