using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
namespace DingDongBiomeHomes;
// Owner item 8 reference: "Этапы цепочки" — a framed card with up to three stages (previous,
// current, next) on an arrowed track: done = green with a check, current = gold ring, next = grey.
// Arrows at the ends mean more stages exist; every stage title is still shown on hover.
// Text layout is cached in Recalculate, so DrawSelf does not allocate.
internal sealed class ResidentStoryTrail:UIElement
{
    private const float LabelScale=.58f,Pad=12f;
    private readonly int completed,count,first,shown;
    private readonly string[] titles,numbers;
    private string[][] labels=Array.Empty<string[]>();
    private float[] labelScales=Array.Empty<float>();
    private float[][] labelWidths=Array.Empty<float[]>();
    private readonly string header;
    private float headerScale=.72f;
    internal ResidentStoryTrail(string key,int completed)
    {
        this.completed=completed;Width.Set(0,1);Height.Set(104,0);
        count=Math.Max(0,PersonalResidentQuestCatalog.Length(key));shown=Math.Min(3,count);
        first=Math.Clamp(completed-1,0,Math.Max(0,count-shown));
        titles=new string[shown];numbers=new string[shown];
        for(int i=0;i<shown;i++){titles[i]=PersonalResidentQuestCatalog.Step(key+":"+(first+i))?.Title??"";numbers[i]=(first+i+1).ToString();}
        header=BookUiText.Get("StagesTitle");
    }
    public override void Recalculate()
    {
        base.Recalculate();
        var font=FontAssets.MouseText.Value;float width=GetDimensions().Width;
        headerScale=Math.Clamp((width-Pad*2)/Math.Max(1f,font.MeasureString(header).X),.5f,.72f);
        float cell=shown>0?(width-Pad*2)/shown:width;
        labels=new string[shown][];labelWidths=new float[shown][];labelScales=new float[shown];
        for(int i=0;i<shown;i++){
            // 29.10 (owner shot): long words were cut mid-word; the label now shrinks until its
            // longest word fits the cell, and only a third line is dropped (full title on hover).
            float longest=1f;foreach(string w in titles[i].Split(' '))longest=Math.Max(longest,font.MeasureString(w).X);
            float s=Math.Clamp((cell-6)/longest,.40f,LabelScale);labelScales[i]=s;
            labels[i]=Wrap(font,titles[i],cell-6,2,s);labelWidths[i]=new float[labels[i].Length];
            for(int j=0;j<labels[i].Length;j++)labelWidths[i][j]=font.MeasureString(labels[i][j]).X*s;}
    }
    private static string[] Wrap(DynamicSpriteFont font,string text,float width,int maxLines,float scale)
    {
        float LabelScale=scale;
        var output=new List<string>();string line="";
        foreach(string word in text.Split(' '))
        {
            string next=line.Length==0?word:line+" "+word;
            if(line.Length>0&&font.MeasureString(next).X*LabelScale>width){output.Add(line);line=word;}else line=next;
        }
        if(line.Length>0)output.Add(line);
        for(int i=0;i<output.Count;i++)
            while(output[i].Length>1&&font.MeasureString(output[i]).X*LabelScale>width)output[i]=output[i][..^1]; // full title stays on hover
        if(output.Count>maxLines){output.RemoveRange(maxLines,output.Count-maxLines);output[maxLines-1]=output[maxLines-1].TrimEnd('.',',')+"…";}
        return output.ToArray();
    }
    private static void Line(SpriteBatch batch,Vector2 from,Vector2 to,Color color,float thickness)
    {
        Vector2 delta=to-from;
        batch.Draw(TextureAssets.MagicPixel.Value,from,new Rectangle(0,0,1,1),color,(float)Math.Atan2(delta.Y,delta.X),new Vector2(0,.5f),new Vector2(delta.Length(),thickness),SpriteEffects.None,0);
    }
    protected override void DrawSelf(SpriteBatch batch)
    {
        var a=GetDimensions();var font=FontAssets.MouseText.Value;
        DingDongUiAssets.DrawBookCard(batch,a.ToRectangle(),Color.White*.9f);
        batch.DrawString(font,header,new Vector2(a.X+Pad,a.Y+7),ResidentBookJournal.Ink,0,Vector2.Zero,headerScale,SpriteEffects.None,0);
        if(shown==0)return;
        float cell=(a.Width-Pad*2)/shown,cy=a.Y+46;
        var track=new Color(150,128,90);
        Line(batch,new Vector2(a.X+Pad,cy),new Vector2(a.X+a.Width-Pad,cy),track,2);
        if(first>0){Line(batch,new Vector2(a.X+Pad,cy),new Vector2(a.X+Pad+7,cy-5),track,2);Line(batch,new Vector2(a.X+Pad,cy),new Vector2(a.X+Pad+7,cy+5),track,2);}
        if(first+shown<count){float r=a.X+a.Width-Pad;Line(batch,new Vector2(r,cy),new Vector2(r-7,cy-5),track,2);Line(batch,new Vector2(r,cy),new Vector2(r-7,cy+5),track,2);}
        for(int i=0;i<shown;i++)
        {
            int stage=first+i;float cx=a.X+Pad+cell*(i+.5f);
            bool done=stage<completed,current=stage==completed;
            var box=new Rectangle((int)cx-15,(int)cy-16,30,32);
            if(current){var ring=box;ring.Inflate(5,5);BookSkin.Draw(batch,ring,BookGlyph.Chapter,new Color(236,176,52));}
            BookSkin.Draw(batch,box,BookGlyph.Chapter,done?new Color(112,196,82):current?Color.White:new Color(150,140,120));
            if(done){
                var check=new Color(32,72,24);
                Line(batch,new Vector2(cx-7,cy),new Vector2(cx-2,cy+5),check,3);Line(batch,new Vector2(cx-3,cy+5),new Vector2(cx+7,cy-6),check,3);
            }else{
                var size=font.MeasureString(numbers[i])*.8f;
                batch.DrawString(font,numbers[i],new Vector2(cx,cy)-size*.5f,current?ResidentBookJournal.Ink:new Color(110,100,84),0,Vector2.Zero,.8f,SpriteEffects.None,0);
            }
            if(i<labels.Length)
                for(int j=0;j<labels[i].Length;j++)
                    batch.DrawString(font,labels[i][j],new Vector2(cx-labelWidths[i][j]*.5f,cy+21+j*14),stage>completed?ResidentBookJournal.Muted*.8f:ResidentBookJournal.Ink,0,Vector2.Zero,i<labelScales.Length?labelScales[i]:LabelScale,SpriteEffects.None,0);
            if(IsMouseHovering&&Main.MouseScreen.X>=a.X+Pad+i*cell&&Main.MouseScreen.X<a.X+Pad+(i+1)*cell)Main.hoverItemName=titles[i];
        }
    }
}
