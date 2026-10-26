using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

public enum OceanMotion { BottomScuttle, Dart, Orbit, Pulse, Inflate, Ribbon, SailCharge, Guard }
public readonly record struct OceanFaunaSpec(int Width, int Height, int Life, int Damage, int Defense,
    OceanMotion Motion, OceanExpeditionZone First, OceanExpeditionZone Last, bool Hardmode, int Loot, int NativeLoot);

public abstract class OceanFauna : ModNPC
{
    protected abstract OceanFaunaSpec Spec { get; }
    public override string Texture => "DingDongBiomeHomes/OceanExpeditions/Assets/" + Name;
    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Spec.Motion is OceanMotion.BottomScuttle or OceanMotion.Dart or OceanMotion.SailCharge ? 1 : 4;
    public override void SetDefaults()
    {
        var s = Spec; NPC.width = s.Width; NPC.height = s.Height;
        NPC.lifeMax = Main.hardMode && !s.Hardmode ? (int)(s.Life * 2.3f) : s.Life;
        NPC.damage = Main.hardMode && !s.Hardmode ? (int)(s.Damage * 1.4f) : s.Damage;
        NPC.defense = s.Defense + (Main.hardMode && !s.Hardmode ? 8 : 0);
        NPC.aiStyle = -1; NPC.noGravity = true; NPC.noTileCollide = false; NPC.knockBackResist = .45f;
        NPC.npcSlots = s.Width >= 100 ? 2 : 1; NPC.value = Item.buyPrice(silver: Math.Clamp(s.Life / 40, 1, 25));
        NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
        Banner = ModContent.NPCType<CoralPincher>(); BannerItem = ModContent.ItemType<ReefFaunaBanner>();
    }
    public override void SetBestiary(BestiaryDatabase db, BestiaryEntry entry) =>
        entry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean);
    public override float SpawnChance(NPCSpawnInfo info)
    {
        if (!info.Water || info.PlayerInTown || (Spec.Hardmode && !Main.hardMode) || NPC.CountNPCS(Type) >= 3 ||
            !OceanExpeditionWorldSystem.TryGetZone(info.SpawnTileX, info.SpawnTileY, out var z) || z < Spec.First || z > Spec.Last) return 0;
        return Spec.Motion is OceanMotion.SailCharge or OceanMotion.Guard ? .07f : .16f;
    }
    public override void FindFrame(int height)
    {
        NPC.frameCounter += .13 + Math.Min(.15f, NPC.velocity.Length() * .025f);
        if(Main.npcFrameCount[Type]==1) { NPC.frame.Y=0; return; }
        NPC.frame.Y = Spec.Motion == OceanMotion.Inflate && NPC.ai[1] > .5f ? 2 * height : ((int)NPC.frameCounter % 4) * height;
    }
    public override void AI()
    {
        NPC.direction = NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;
        if (Spec.Motion == OceanMotion.Pulse) Lighting.AddLight(NPC.Center, .10f, .38f, .4f);
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (Spec.Motion == OceanMotion.Inflate)
            { NPC.damage = (int)(NPC.defDamage * (NPC.ai[1] > .5f ? 1.35f : .75f)); NPC.defense = NPC.defDefense + (NPC.ai[1] > .5f ? 14 : 0); }
            if (Spec.Motion == OceanMotion.Guard) NPC.defense = NPC.defDefense + (NPC.ai[2] > .5f ? 20 : 0);
            return;
        }
        NPC.TargetClosest(false); var target = Main.player[Math.Clamp(NPC.target, 0, Main.maxPlayers - 1)];
        if (!NPC.wet) { NPC.velocity.X *= .96f; NPC.velocity.Y = Math.Min(6f, NPC.velocity.Y + .22f); NPC.EncourageDespawn(120); return; }
        if (!target.active || target.dead || !target.wet || Vector2.DistanceSquared(target.Center, NPC.Center) > 1500 * 1500)
        { NPC.velocity *= .98f; NPC.EncourageDespawn(150); return; }
        Vector2 delta = target.Center - NPC.Center;
        float distance = delta.Length(); Vector2 aim = distance > .01f ? delta / distance : Vector2.UnitX;
        int t = (int)NPC.ai[0]++ % 360; NPC.ai[1] = 0; NPC.damage = NPC.defDamage; NPC.defense = NPC.defDefense;
        Vector2 wanted;
        switch (Spec.Motion)
        {
            case OceanMotion.BottomScuttle:
                wanted = new(aim.X * 2.2f, NPC.collideY ? -2.5f : 1.4f); break;
            case OceanMotion.Dart:
                wanted = aim * (t % 100 < 22 ? 6f : 1.2f); break;
            case OceanMotion.Orbit:
                wanted = aim * (distance > 240 ? 2.6f : -.8f) + new Vector2(-aim.Y, aim.X) * 2.3f; break;
            case OceanMotion.Pulse:
                wanted = aim * (t % 90 < 25 ? 3.6f : .5f); wanted.Y += (float)Math.Sin(t * .05) * 1.4f; break;
            case OceanMotion.Inflate:
                bool inflated = t >= 130 && t < 235; NPC.ai[1] = inflated ? 1 : 0;
                NPC.defense += inflated ? 14 : 0; NPC.damage = (int)(NPC.defDamage * (inflated ? 1.35f : .75f));
                wanted = aim * (inflated ? .65f : 2.1f); break;
            case OceanMotion.Ribbon:
                wanted = aim * 3.5f + new Vector2(-aim.Y, aim.X) * (float)Math.Sin(t * .055) * 2f; break;
            case OceanMotion.SailCharge:
                NPC.ai[1] = t >= 150 && t < 190 ? 1 : 0;
                if (t == 190) { NPC.ai[2] = aim.X; NPC.ai[3] = aim.Y; }
                wanted = t >= 190 && t < 230 ? new Vector2(NPC.ai[2],NPC.ai[3]) * 8.5f : aim * (t < 150 ? 2.7f : .8f); break;
            default:
                bool shell = t < 120; NPC.ai[2] = shell ? 1 : 0; NPC.defense += shell ? 20 : 0; NPC.ai[1] = t >= 120 && t < 150 ? 1 : 0;
                wanted = aim * (shell ? .8f : 2.2f);
                if (t == 150 && distance < 700 && Collision.CanHitLine(NPC.position,NPC.width,NPC.height,target.position,target.width,target.height))
                    for (int i=-1;i<=1;i++) Projectile.NewProjectile(NPC.GetSource_FromAI(),NPC.Center,aim.RotatedBy(i*.20f)*4.5f,ModContent.ProjectileType<BrineSpine>(),15,0,Main.myPlayer);
                break;
        }
        NPC.velocity = Vector2.Lerp(NPC.velocity, wanted, .06f);
        if (NPC.collideX) { NPC.velocity.X *= -.7f; NPC.velocity.Y -= 1.5f; }
        if (NPC.collideY && Spec.Motion != OceanMotion.BottomScuttle) NPC.velocity.Y *= -.7f;
        if (t % 30 == 0 || t is 130 or 150 or 190 or 230 or 235) NPC.netUpdate = true;
    }
    public override Color? GetAlpha(Color c) => NPC.ai[1] > .5f ? Color.Lerp(c,Color.White,.4f) : null;
    public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch,Vector2 screen,Color c)
    {
        if(Spec.Motion==OceanMotion.BottomScuttle) { OceanCrustaceanRig.Draw(NPC,batch,screen,c,Name);return false; }
        if(Spec.Motion is OceanMotion.Dart or OceanMotion.SailCharge)
        { OceanSegmentAnimator.Draw(NPC,batch,screen,c,Spec.Motion==OceanMotion.Dart?.62f:.42f);return false; }
        return true;
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.Common(ModContent.ItemType<SeaEssence>(),2,1,3));
        loot.Add(ItemDropRule.Common(Spec.Loot,2,1,2));
        loot.Add(ItemDropRule.Common(Spec.NativeLoot,4,1,2));
    }
}
public sealed class CoralPincher : OceanFauna { protected override OceanFaunaSpec Spec => new(42,30,95,16,9,OceanMotion.BottomScuttle,OceanExpeditionZone.Shelf,OceanExpeditionZone.OpenSea,false,ModContent.ItemType<CoralClaw>(),ItemID.Coral); }
public sealed class GlassfinDarter : OceanFauna { protected override OceanFaunaSpec Spec => new(36,18,55,11,2,OceanMotion.Dart,OceanExpeditionZone.Shelf,OceanExpeditionZone.OpenSea,false,ModContent.ItemType<GlassFin>(),ItemID.SilverCoin); }
public sealed class RibbonSkate : OceanFauna { protected override OceanFaunaSpec Spec => new(96,40,260,25,9,OceanMotion.Orbit,OceanExpeditionZone.OpenSea,OceanExpeditionZone.DeepSea,false,ModContent.ItemType<RibbonGill>(),ItemID.SharkFin); }
public sealed class LanternJelly : OceanFauna { protected override OceanFaunaSpec Spec => new(42,62,210,23,5,OceanMotion.Pulse,OceanExpeditionZone.DeepSea,OceanExpeditionZone.HadalSea,false,ModContent.ItemType<LuminousGel>(),ItemID.Glowstick); }
public sealed class BrinePuffer : OceanFauna { protected override OceanFaunaSpec Spec => new(52,44,320,28,12,OceanMotion.Inflate,OceanExpeditionZone.OpenSea,OceanExpeditionZone.DeepSea,false,ModContent.ItemType<BrineBladder>(),ItemID.Gel); }
public sealed class SiltRibbon : OceanFauna
{
    protected override OceanFaunaSpec Spec => new(144,38,430,34,15,OceanMotion.Ribbon,OceanExpeditionZone.DeepSea,OceanExpeditionZone.HadalSea,false,ModContent.ItemType<SiltWhisker>(),ItemID.Bone);
    public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch,Vector2 screen,Color c) { OceanSegmentAnimator.Draw(NPC,batch,screen,c);return false; }
}
public sealed class SailfinHunter : OceanFauna { protected override OceanFaunaSpec Spec => new(132,54,650,42,17,OceanMotion.SailCharge,OceanExpeditionZone.OpenSea,OceanExpeditionZone.DeepSea,false,ModContent.ItemType<Sailbone>(),ItemID.SharkFin); }
public sealed class VentNautilus : OceanFauna { protected override OceanFaunaSpec Spec => new(78,70,1850,55,28,OceanMotion.Guard,OceanExpeditionZone.HadalSea,OceanExpeditionZone.HadalSea,true,ModContent.ItemType<VentCore>(),ItemID.Obsidian); }

internal static class OceanOpenWater
{
    internal static bool AdmitsGiant(int x,int y)
    {
        // A 3072 x 1536 pixel clear envelope is several times the creature's
        // body and larger than a normal gameplay viewport in both directions.
        const int rx=96,ry=48;
        if(x-rx<20||y-ry<20||x+rx>=Main.maxTilesX-20||y+ry>=Main.maxTilesY-20)return false;
        for(int xx=x-rx;xx<=x+rx;xx+=10)
        for(int yy=y-ry;yy<=y+ry;yy+=10)
        {
            Tile tile=Main.tile[xx,yy];
            if(tile.LiquidType!=LiquidID.Water||tile.LiquidAmount<220||tile.HasTile&&Main.tileSolid[tile.TileType]&&!Main.tileSolidTop[tile.TileType])return false;
        }
        return true;
    }
}
