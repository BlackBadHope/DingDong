using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
namespace DingDongBiomeHomes;
internal enum BookGlyph { Quest,Combat,Residents,Help,Close,Action,Chapter,Residence }
internal static class BookSkin
{
    private static Rectangle Region(BookGlyph glyph)=>glyph switch {
        BookGlyph.Quest=>new(120,165,210,176),BookGlyph.Combat=>new(558,155,190,178),
        BookGlyph.Residents=>new(1010,155,210,181),BookGlyph.Help=>new(1437,164,218,177),
        BookGlyph.Close=>new(143,607,157,158),BookGlyph.Action=>new(423,627,534,120),
        BookGlyph.Chapter=>new(1032,575,212,219),_=>new(1336,570,403,225)};
    internal static void Draw(SpriteBatch batch,Rectangle destination,BookGlyph glyph,Color tint)
    {
        var texture=DingDongUiAssets.BookControls?.Value;if(texture==null)return;
        var source=Region(glyph);
        float fit=Math.Min(destination.Width/(float)source.Width,destination.Height/(float)source.Height);
        var size=new Point((int)(source.Width*fit),(int)(source.Height*fit));
        var area=new Rectangle(destination.Center.X-size.X/2,destination.Center.Y-size.Y/2,size.X,size.Y);
        batch.Draw(texture,area,source,tint);
    }
    internal static void ActionFrame(SpriteBatch batch,Rectangle destination,Color tint)
    {
        var texture=DingDongUiAssets.BookControls?.Value;if(texture==null)return;
        var source=Region(BookGlyph.Action);const int sc=48;int dc=Math.Min(14,destination.Height/3);
        for(int y=0;y<3;y++)for(int x=0;x<3;x++){
            int sx=x==0?source.X:x==1?source.X+sc:source.Right-sc,sy=y==0?source.Y:y==1?source.Y+sc:source.Bottom-sc;
            int sw=x==1?source.Width-sc*2:sc,sh=y==1?source.Height-sc*2:sc;
            int dx=x==0?destination.X:x==1?destination.X+dc:destination.Right-dc,dy=y==0?destination.Y:y==1?destination.Y+dc:destination.Bottom-dc;
            int dw=x==1?destination.Width-dc*2:dc,dh=y==1?destination.Height-dc*2:dc;
            if(dw>0&&dh>0)batch.Draw(texture,new Rectangle(dx,dy,dw,dh),new Rectangle(sx,sy,sw,sh),tint);
        }
    }
}
internal sealed class BookAssetIcon: UIElement
{
    private readonly BookGlyph glyph;
    internal BookAssetIcon(BookGlyph glyph){this.glyph=glyph;IgnoresMouseInteraction=true;}
    protected override void DrawSelf(SpriteBatch batch)=>BookSkin.Draw(batch,GetDimensions().ToRectangle(),glyph,Color.White);
}
