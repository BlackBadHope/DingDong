using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace DingDongBiomeHomes.OceanExpeditions;

// Head-to-tail bones keep the head anchored in either facing direction.
// Only the tail receives the travelling wave. Collision remains one NPC.
internal static class OceanSegmentAnimator
{
    internal static float TailAngle(float fraction,float phase,float amplitude)
    {
        float tail=MathHelper.Clamp((fraction-.38f)/.62f,0,1);
        return MathF.Sin(phase-tail*3.4f)*amplitude*tail*tail;
    }
    internal static void Draw(NPC npc, SpriteBatch batch, Vector2 screen, Color color,float amplitude=.28f)
        => DrawAt(npc,batch,screen,color,Main.GlobalTimeWrappedHourly*(5.2f+Math.Min(3,npc.velocity.Length()*.45f))+npc.whoAmI*.37f,amplitude);
    internal static void DrawAt(NPC npc,SpriteBatch batch,Vector2 screen,Color color,float phase,float amplitude=.28f)
    {
        Texture2D texture=TextureAssets.Npc[npc.type].Value;
        int h=texture.Height/Math.Max(1,Main.npcFrameCount[npc.type]);
        int width=texture.Width,parts=Math.Min(16,width/2);
        bool right=npc.spriteDirection>=0;
        float sign=right?-1:1;
        Vector2 joint=npc.Center-screen+new Vector2(-sign*width*npc.scale*.5f,0).RotatedBy(npc.rotation);
        Color alpha=npc.GetAlpha(color);
        for(int i=0;i<parts;i++)
        {
            int x=i*width/parts,end=(i+1)*width/parts;
            float angle=npc.rotation+sign*TailAngle(i/(float)(parts-1),phase,amplitude);
            Rectangle source=new(x,npc.frame.Y,Math.Min(width-x,end-x+1),h);
            batch.Draw(texture,joint,source,alpha,angle,new Vector2(right?source.Width:0,h*.5f),npc.scale,right?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
            joint+=new Vector2(MathF.Cos(angle),MathF.Sin(angle))*sign*(end-x)*npc.scale;
        }
    }
}
