using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace DingDongBiomeHomes.OceanExpeditions;

// One immutable torso, six two-bone walking legs and two claws. Joint transforms
// animate the same anatomy at every phase; damage and collision stay on one NPC.
internal static class OceanCrustaceanRig
{
    internal const int WalkingLegs=6, Claws=2;
    internal static void Draw(NPC npc,SpriteBatch batch,Vector2 screen,Color color,string name)
        => DrawAt(npc,batch,screen,color,name,(float)npc.frameCounter);
    internal static void DrawAt(NPC npc,SpriteBatch batch,Vector2 screen,Color color,string name,float gait)
    {
        const string root="DingDongBiomeHomes/OceanExpeditions/Assets/";
        Texture2D torso=ModContent.Request<Texture2D>(root+name+"Torso",AssetRequestMode.ImmediateLoad).Value;
        Texture2D leg=ModContent.Request<Texture2D>(root+name+"Leg",AssetRequestMode.ImmediateLoad).Value;
        Texture2D claw=ModContent.Request<Texture2D>(root+name+"Claw",AssetRequestMode.ImmediateLoad).Value;
        bool right=npc.spriteDirection>=0;float facing=right?-1:1;
        float stride=MathHelper.Clamp(Math.Abs(npc.velocity.X)/1.8f,.12f,1f);
        Vector2 center=npc.Center-screen+new Vector2(0,-2+MathF.Sin(gait*2)*.5f*stride);
        Color alpha=npc.GetAlpha(color);
        Vector2 Mirror(Vector2 p)=>new(p.X*facing,p.Y);
        void Part(Texture2D tex,Vector2 at,Rectangle source,Vector2 origin,float angle,Color tint,float scale=1)
        {
            if(right)origin.X=source.Width-origin.X;
            batch.Draw(tex,center+Mirror(at)*npc.scale,source,tint,angle*facing,origin,npc.scale*scale,right?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
        }
        void Legs(bool far)
        {
            for(int i=0;i<3;i++)
            {
                float phase=gait+i*2.0944f+(far?MathHelper.Pi:0);
                float hip=(i-1)*.25f+MathF.Sin(phase)*.42f*stride;
                float knee=-.15f-Math.Max(0,MathF.Cos(phase))*.65f*stride;
                Vector2 at=new(-6+i*6,far?1:4);
                int ux=Math.Max(2,(int)(leg.Width*.62f)),uy=Math.Max(2,(int)(leg.Height*.60f));
                int lx=(int)(leg.Width*.48f),ly=(int)(leg.Height*.44f);
                Vector2 upperRoot=new(leg.Width*.08f,leg.Height*.10f);
                Vector2 hinge=new(leg.Width*.54f,leg.Height*.49f);
                Color tint=far?alpha*.65f:alpha;
                Part(leg,at,new Rectangle(0,0,ux,uy),upperRoot,hip,tint);
                Vector2 joint=at+(hinge-upperRoot).RotatedBy(hip);
                Part(leg,joint,new Rectangle(lx,ly,leg.Width-lx,leg.Height-ly),hinge-new Vector2(lx,ly),hip+knee,tint);
            }
        }
        void Claw(bool far)
        {
            float angle=-.12f+MathF.Sin(gait+(far?1.7f:0))*.18f*stride;
            Part(claw,new Vector2(far?-7:-9,far?1:6),claw.Bounds,new Vector2(claw.Width-1,claw.Height*.6f),angle,far?alpha*.7f:alpha,far?.7f:1f);
        }
        Legs(true);Claw(true);
        Part(torso,new Vector2(1,-5),torso.Bounds,torso.Size()*.5f,MathF.Sin(gait)*.025f*stride,alpha);
        Legs(false);Claw(false);
    }
}
