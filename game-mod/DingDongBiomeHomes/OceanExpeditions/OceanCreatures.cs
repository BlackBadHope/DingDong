using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

public abstract class OceanGiant : ModNPC
{
    protected abstract bool Hadal { get; }
    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
    public override void SetDefaults()
    {
        NPC.width = Hadal ? 400 : 352; NPC.height = Hadal ? 148 : 140;
        NPC.scale = Hadal ? 2.3f : 2f;
        NPC.lifeMax = Hadal ? 9500 : Main.hardMode ? 4200 : 1350;
        NPC.damage = Hadal ? 75 : Main.hardMode ? 52 : 27;
        NPC.defense = Hadal ? 32 : Main.hardMode ? 22 : 10;
        NPC.aiStyle = -1; NPC.noGravity = true; NPC.noTileCollide = false;
        NPC.npcSlots = 3; NPC.knockBackResist = .05f;
        NPC.value = Item.buyPrice(silver: Hadal ? 90 : 35);
        NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
        Banner = Hadal ? ModContent.NPCType<PelagicTitan>() : Type; BannerItem = ModContent.ItemType<OceanGiantBanner>();
    }
    public override float SpawnChance(NPCSpawnInfo info)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !info.Water || info.PlayerInTown ||
            (Hadal && !Main.hardMode) || NPC.CountNPCS(Type) >= 1 ||
            !OceanExpeditionWorldSystem.TryGetZone(info.SpawnTileX, info.SpawnTileY, out var zone)) return 0;
        if (Hadal ? zone != OceanExpeditionZone.HadalSea : zone is not (OceanExpeditionZone.OpenSea or OceanExpeditionZone.DeepSea)) return 0;
        if (!OceanOpenWater.AdmitsGiant(info.SpawnTileX, info.SpawnTileY)) return 0;
        // A giant must fit where it spawns; no wall-phasing or contact traps.
        Rectangle box = new(info.SpawnTileX * 16 - NPC.width / 2, info.SpawnTileY * 16 - NPC.height / 2, NPC.width, NPC.height);
        if (Collision.SolidCollision(box.TopLeft(), box.Width, box.Height)) return 0;
        return Hadal ? .035f : .045f;
    }
    public override void SetBestiary(BestiaryDatabase db, BestiaryEntry entry)
    {
        entry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean);
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.DingDongBiomeHomes.OceanExpeditions." + Name + "Lore"));
    }
    public override void AI()
    {
        Lighting.AddLight(NPC.Center,.07f,.20f,.26f);
        NPC.direction = NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;
        NPC.rotation = Math.Clamp(NPC.velocity.Y * NPC.direction * .018f, -.16f, .16f);
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        NPC.TargetClosest(false);
        Player target = Main.player[Math.Clamp(NPC.target, 0, Main.maxPlayers - 1)];
        if (!NPC.wet)
        { NPC.velocity.X *= .95f; NPC.velocity.Y = Math.Min(7, NPC.velocity.Y + .2f); NPC.EncourageDespawn(90); return; }
        if (!target.active || target.dead || !target.wet || Vector2.DistanceSquared(target.Center, NPC.Center) > 2000 * 2000 ||
            !OceanExpeditionWorldSystem.TryGetZone((int)target.Center.X / 16, (int)target.Center.Y / 16, out var zone) || zone == OceanExpeditionZone.AirGrotto)
        { NPC.velocity *= .985f; NPC.EncourageDespawn(180); return; }
        int cycle = (int)NPC.ai[0]++ % (Hadal ? 300 : 380);
        bool warn = cycle >= 180 && cycle < 225, charge = cycle >= 225 && cycle < 265;
        NPC.ai[1] = warn ? 1 : 0;
        Vector2 direction = target.Center - NPC.Center;
        if (direction.LengthSquared() > .01f) direction.Normalize(); else direction = Vector2.UnitX;
        float speed = charge ? Hadal ? 11f : 8.5f : warn ? 1.2f : Hadal ? 3.8f : 2.8f;
        Vector2 desired = direction * speed;
        if (!charge) desired.Y += (float)Math.Sin(NPC.ai[0] * .025f) * .8f;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, charge ? .10f : .025f);
        if (NPC.collideX) { NPC.velocity.X *= -.8f; NPC.velocity.Y = -3f; }
        if (NPC.collideY) NPC.velocity.Y *= -.8f;
        if (Hadal && cycle == 225 && Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
            for (int i = -1; i <= 1; i++)
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, direction.RotatedBy(i * .22f) * 5.5f,
                    ModContent.ProjectileType<BrineSpine>(), 22, 0, Main.myPlayer);
        if (cycle is 180 or 225 or 265 || cycle % 30 == 0) NPC.netUpdate = true;
    }
    public override Color? GetAlpha(Color color) => NPC.ai[1] > .5f ? Color.Lerp(color, Color.Cyan, .55f) : Color.Lerp(color,new Color(155,195,210),.20f);
    public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch, Vector2 screen, Color color)
    { OceanSegmentAnimator.Draw(NPC, batch, screen, color); return false; }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.Common(Hadal ? ModContent.ItemType<HadalLens>() : ModContent.ItemType<PelagicScale>(), 1, Hadal ? 3 : 2, Hadal ? 6 : 4));
        loot.Add(ItemDropRule.Common(ItemID.GillsPotion, 3, 1, 2));
        loot.Add(ItemDropRule.Common(ModContent.ItemType<SeaEssence>(), 1, 4, 7));
    }
}
public sealed class PelagicTitan : OceanGiant
{
    protected override bool Hadal => false;
    public override string Texture => "DingDongBiomeHomes/OceanExpeditions/Assets/PelagicTitan";
}
public sealed class HadalMaw : OceanGiant
{
    protected override bool Hadal => true;
    public override string Texture => "DingDongBiomeHomes/OceanExpeditions/Assets/HadalMaw";
}
public sealed class SaltbackCustodian : ModNPC
{
    public override string Texture => "DingDongBiomeHomes/OceanExpeditions/Assets/SaltbackCustodian";
    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
    public override void SetDefaults()
    {
        NPC.width = 44; NPC.height = 30; NPC.aiStyle = -1; NPC.defense = 14; NPC.knockBackResist = .3f;
        NPC.lifeMax = Main.hardMode ? 700 : 160; NPC.damage = Main.hardMode ? 48 : 20;
        NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
        Banner = Type; BannerItem = ModContent.ItemType<GrottoBanner>();
    }
    public override float SpawnChance(NPCSpawnInfo info) => !info.Water && !info.PlayerInTown &&
        OceanExpeditionWorldSystem.TryGetZone(info.SpawnTileX, info.SpawnTileY - 2, out var zone) &&
        zone == OceanExpeditionZone.AirGrotto ? .30f : 0;
    public override void FindFrame(int height) { NPC.frameCounter += .03f+Math.Abs(NPC.velocity.X)*.14f; NPC.frame.Y=0; }
    public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch,Vector2 screen,Color c)
    { OceanCrustaceanRig.Draw(NPC,batch,screen,c,Name);return false; }
    public override void AI()
    {
        NPC.direction = NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        NPC.TargetClosest(false); var p = Main.player[Math.Clamp(NPC.target,0,Main.maxPlayers-1)];
        if (!p.active || p.dead) { NPC.EncourageDespawn(90); return; }
        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(p.Center.X-NPC.Center.X)*1.8f,.05f);
        int t=(int)NPC.ai[0]++%120; if(t==90&&NPC.collideY) { NPC.velocity.Y=-5.5f;NPC.netUpdate=true; }
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    { loot.Add(ItemDropRule.Common(ModContent.ItemType<GrottoGeode>(),2,1,2)); loot.Add(ItemDropRule.Common(ModContent.ItemType<SaltCrystal>(),2)); loot.Add(ItemDropRule.Common(ModContent.ItemType<SeaEssence>(),2)); }
}
public sealed class GlasswingSkimmer : ModNPC
{
    public override string Texture => "DingDongBiomeHomes/OceanExpeditions/Assets/GlasswingSkimmer";
    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;
    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.CaveBat); AIType = NPCID.CaveBat; AnimationType = 0;
        NPC.lifeMax = Main.hardMode ? 320 : 90; NPC.damage = Main.hardMode ? 38 : 16;
        Banner = ModContent.NPCType<SaltbackCustodian>(); BannerItem = ModContent.ItemType<GrottoBanner>();
    }
    public override float SpawnChance(NPCSpawnInfo info) => !info.Water && !info.PlayerInTown &&
        OceanExpeditionWorldSystem.TryGetZone(info.SpawnTileX, info.SpawnTileY - 2, out var zone) &&
        zone == OceanExpeditionZone.AirGrotto ? .22f : 0;
    public override void FindFrame(int height) { NPC.frameCounter += .25; NPC.frame.Y = (int)NPC.frameCounter % 4 * height; }
    public override void ModifyNPCLoot(NPCLoot loot)
    { loot.Add(ItemDropRule.Common(ModContent.ItemType<GrottoGeode>(),3)); loot.Add(ItemDropRule.Common(ModContent.ItemType<GlassWing>(),2)); loot.Add(ItemDropRule.Common(ModContent.ItemType<SeaEssence>(),2)); }
}
public sealed class BrineSpine : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;
    public override void SetDefaults()
    { Projectile.width = Projectile.height = 12; Projectile.hostile = true; Projectile.friendly = false; Projectile.ignoreWater = true; Projectile.tileCollide = true; Projectile.timeLeft = 150; Projectile.penetrate = 1; }
    public override void AI() => Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
}
