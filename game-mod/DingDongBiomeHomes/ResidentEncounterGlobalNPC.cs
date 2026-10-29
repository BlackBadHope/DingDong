using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace DingDongBiomeHomes;

public sealed class ResidentEncounterGlobalNPC : GlobalNPC
{
    public override bool CheckActive(NPC npc) => !ResidentEncounterSystem.TryKey(npc,out string key) || !ResidentEncounterSystem.TryState(key,out _) || !ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort;
    public override void GetChat(NPC npc,ref string chat)
    {if(ResidentEncounterSystem.TryKey(npc,out string key)&&ResidentEncounterSystem.IsPending(key))ResidentEncounterSystem.RequestTalk(npc);}
    public override void OnKill(NPC npc){if(Main.netMode!=NetmodeID.MultiplayerClient&&ResidentEncounterSystem.TryKey(npc,out string key))ResidentEncounterSystem.Died(key);}
    public override bool PreAI(NPC npc)
    {
        if(ResidentEncounterSystem.TryKey(npc,out string key)&&ResidentEncounterSystem.IsPending(key)){npc.homeless=true;npc.homeTileX=npc.homeTileY=-1;}
        // A town NPC with a home outside the world crashes vanilla AI_007_TryForcingSitting on teleport.
        else if(npc.townNPC&&!npc.homeless&&(npc.homeTileX<10||npc.homeTileY<10||npc.homeTileX>=Main.maxTilesX-10||npc.homeTileY>=Main.maxTilesY-10))npc.homeless=true;
        if(!ResidentEncounterSystem.IsPending("Terraria/Dryad")||npc.type!=NPCID.Dryad)return true;
        if(!ResidentEncounterSystem.TryState("Terraria/Dryad",out var s))return true;
        Point anchor=Main.dayTime?s.Day:s.Night;
        if(anchor==Point.Zero)return true;
        // Peaceful wander state has no vanilla combat aura or attack projectiles.
        npc.dontTakeDamage=true;npc.damage=0;npc.homeless=true;npc.ai[0]=0;npc.ai[1]=0;
        float dx=anchor.X*16+8-npc.Center.X;
        int direction=Math.Abs(dx)>240?Math.Sign(dx):((Main.GameUpdateCount/240)%2==0?1:-1);
        npc.direction=npc.spriteDirection=direction;npc.velocity.X=MathHelper.Lerp(npc.velocity.X,direction*.55f,.06f);
        if(npc.collideX&&npc.collideY)npc.velocity.Y=-4.5f;
        return false;
    }
}

public sealed class ResidentEncounterSeasonCompatibility : ModSystem
{
    public override void Load(){On_NPC.AI+=RunAI;On_NPC.UpdateNPC+=RunUpdate;}
    public override void Unload(){On_NPC.AI-=RunAI;On_NPC.UpdateNPC-=RunUpdate;}
    // Vanilla kills Santa outside Christmas in the update step before AI runs
    // ("Santa Claus was slain" on every respawn), so the update needs the same scope.
    private static void RunUpdate(On_NPC.orig_UpdateNPC orig,NPC npc,int i)
    {
        if(npc.type!=NPCID.SantaClaus||!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort){orig(npc,i);return;}
        bool christmas=Main.xMas;try{Main.xMas=true;orig(npc,i);}finally{Main.xMas=christmas;}
    }
    private static void RunAI(On_NPC.orig_AI orig,NPC npc)
    {
        // Santa is a permanent discoverable resident in this pack. Scope Christmas
        // only to his native AI; preserve the world's date and every other entity.
        if(npc.type!=NPCID.SantaClaus||!ModContent.GetInstance<DingDongServerConfig>().EnableNpcQuestsAndEscort){orig(npc);return;}
        bool christmas=Main.xMas;try{Main.xMas=true;orig(npc);}finally{Main.xMas=christmas;}
    }
}
