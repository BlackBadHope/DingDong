using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace DingDongBiomeHomes;

// Pixel-art sprites (portraits, item glyphs) must be drawn with point sampling, while the
// book text keeps the UI's smooth sampler. Item 8 review: restarting the SpriteBatch for
// every glyph (and boxing its value-type state through reflection) cost a batch flush per
// icon per frame. Sprites inside a BookList are now queued and drawn in ONE point-sampled
// pass after the list's children; only icons outside a book list fall back to a scoped
// restart, which reads reference-type state only (no boxing).
internal static class BookPixelSampler
{
    private static FieldInfo Field(string name)=>typeof(SpriteBatch).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
    private static readonly FieldInfo Blend=Field("blendState"),Sampler=Field("samplerState"),Depth=Field("depthStencilState"),Raster=Field("rasterizerState"),Effect=Field("customEffect");
    internal static bool Ready=>Blend!=null&&Sampler!=null&&Depth!=null&&Raster!=null&&Effect!=null;
    internal static Scope Use(SpriteBatch batch)=>new(batch);

    /// <summary>Draws a pixel sprite: queued when a book list is drawing, otherwise in a scoped point-sampled restart.</summary>
    internal static void Draw(SpriteBatch batch,Texture2D texture,Vector2 position,Rectangle source,Color color,Vector2 origin,float scale)
    {
        if(BookSpriteQueue.TryAdd(texture,position,source,color,origin,scale))return;
        using(Use(batch))batch.Draw(texture,position,source,color,0f,origin,scale,SpriteEffects.None,0f);
    }

    internal readonly struct Scope:IDisposable
    {
        private readonly SpriteBatch batch;
        private readonly BlendState blend;
        private readonly SamplerState sampler;
        private readonly DepthStencilState depth;
        private readonly RasterizerState raster;
        private readonly Effect effect;
        internal Scope(SpriteBatch value)
        {
            this=default;
            if(!Ready||ReferenceEquals(Sampler.GetValue(value),SamplerState.PointClamp))return;
            // UI batches are always Deferred with Main.UIScaleMatrix; only reference-type state is read back.
            batch=value;blend=(BlendState)Blend.GetValue(value);sampler=(SamplerState)Sampler.GetValue(value);
            depth=(DepthStencilState)Depth.GetValue(value);raster=(RasterizerState)Raster.GetValue(value);effect=(Effect)Effect.GetValue(value);
            batch.End();batch.Begin(SpriteSortMode.Deferred,blend,SamplerState.PointClamp,depth,raster,effect,Main.UIScaleMatrix);
        }
        public void Dispose()
        {
            if(batch==null)return;
            batch.End();batch.Begin(SpriteSortMode.Deferred,blend,sampler,depth,raster,effect,Main.UIScaleMatrix);
        }
    }
}

/// <summary>Fixed-capacity queue of pixel sprites collected while a <see cref="BookList"/> draws its children.</summary>
internal static class BookSpriteQueue
{
    private const int Capacity=384;
    private static readonly Texture2D[] Textures=new Texture2D[Capacity];
    private static readonly Vector2[] Positions=new Vector2[Capacity],Origins=new Vector2[Capacity];
    private static readonly Rectangle[] Sources=new Rectangle[Capacity];
    private static readonly Color[] Colors=new Color[Capacity];
    private static readonly float[] Scales=new float[Capacity];
    private static int depth,count;

    internal static bool TryAdd(Texture2D texture,Vector2 position,Rectangle source,Color color,Vector2 origin,float scale)
    {
        if(depth==0||count>=Capacity||texture==null)return false;
        Textures[count]=texture;Positions[count]=position;Sources[count]=source;Colors[count]=color;Origins[count]=origin;Scales[count]=scale;count++;
        return true;
    }

    internal static int Open(){depth++;return count;}

    private static readonly RasterizerState Clipped=new(){CullMode=CullMode.None,ScissorTestEnable=true};

    internal static void Close(SpriteBatch batch,int mark,Rectangle clip)
    {
        depth=Math.Max(0,depth-1);
        if(count<=mark){count=mark;return;}
        batch.End();
        GraphicsDevice device=batch.GraphicsDevice;
        RasterizerState raster=device.RasterizerState;Rectangle scissor=device.ScissorRectangle;
        // Clip to the list explicitly, so the sprites never spill past the leaf whatever the parent state was.
        device.ScissorRectangle=Rectangle.Intersect(clip,scissor);
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.PointClamp,DepthStencilState.None,Clipped,null,Main.UIScaleMatrix);
        for(int i=mark;i<count;i++){batch.Draw(Textures[i],Positions[i],Sources[i],Colors[i],0f,Origins[i],Scales[i],SpriteEffects.None,0f);Textures[i]=null;}
        batch.End();
        device.ScissorRectangle=scissor;
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.AnisotropicClamp,DepthStencilState.None,raster,null,Main.UIScaleMatrix);
        count=mark;
    }
}

/// <summary>Scrolling book leaf: children draw normally, their pixel sprites are flushed once per frame.</summary>
internal sealed class BookList:UIList
{
    internal BookList(float padding){ListPadding=padding;ManualSortMethod=_=>{};}
    protected override void DrawChildren(Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
    {
        int mark=BookSpriteQueue.Open();
        try{base.DrawChildren(batch);}
        finally{BookSpriteQueue.Close(batch,mark,GetClippingRectangle(batch));}
    }
}
