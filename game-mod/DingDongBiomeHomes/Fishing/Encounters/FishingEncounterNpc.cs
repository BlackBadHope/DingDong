using System;
using DingDongBiomeHomes.Fishing.Content;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Encounters;

public abstract class FishingEncounterNpc : ModNPC
{
	protected abstract int EnemyIndex { get; }
	protected FishingEnemySpec Spec => FishingEnemyCatalog.Definitions[EnemyIndex];
	public override string Texture => "Terraria/Images/NPC_" + Spec.TextureNpc;
	public override LocalizedText DisplayName => Language.GetOrRegister("Mods.DingDongBiomeHomes.Fishing.Enemies." + Name + ".DisplayName", () => Spec.NameEn);
	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[Spec.TextureNpc];
	public override void SetDefaults()
	{
		FishingEnemySpec s = Spec;
		NPC.width = s.Width; NPC.height = s.Height;
		NPC.lifeMax = s.Life; NPC.damage = s.Damage; NPC.defense = s.Defense;
		NPC.knockBackResist = s.RequiredProgression < 3 ? .6f : .3f;
		NPC.value = 30 + s.RequiredProgression * 90;
		NPC.noGravity = true; NPC.noTileCollide = false;
		NPC.aiStyle = -1; NPC.npcSlots = 1f;
		NPC.color = s.Tint;
		NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
	}
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => FishingEncounterSystem.SpawnChanceFor(EnemyIndex, spawnInfo);
	public override void AI()
	{
		// Clients extrapolate the synchronized velocity through native NPC movement.
		// Target selection, timers, projectiles, contact damage and phase changes stay server-side.
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			// ai[2]/ai[3] are the authoritative contact/guard projection. Local damage
			// calculations must see the same telegraph and armor window as the server.
			float contact = NPC.ai[2] > 0 && NPC.ai[2] <= 1 ? NPC.ai[2] : 1;
			NPC.damage = Math.Max(1, (int)(NPC.defDamage * (NPC.wet ? contact : 1f / 3f)));
			NPC.defense = NPC.defDefense + Math.Clamp((int)NPC.ai[3], 0, 24);
			return;
		}
		if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active
			|| Main.player[NPC.target].dead) NPC.TargetClosest(false);
		if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Main.player[NPC.target].active
			|| Main.player[NPC.target].dead)
		{
			NPC.velocity *= .98f;
			NPC.EncourageDespawn(120);
			return;
		}
		Player target = Main.player[NPC.target];
		Vector2 toTarget = target.Center - NPC.Center;
		if (toTarget.LengthSquared() > 1600f * 1600f)
		{
			NPC.EncourageDespawn(120);
			NPC.velocity *= .98f;
			return;
		}
		FishingEnemySpec s = Spec;
		if (!NPC.wet)
		{
			NPC.damage = Math.Max(1, NPC.defDamage / 3);
			NPC.velocity = new Vector2(NPC.velocity.X * .96f, Math.Min(6f, NPC.velocity.Y + .25f));
			NPC.ai[0] = 0;
			if (Main.GameUpdateCount % 30 == 0) NPC.netUpdate = true;
			return;
		}
		int tick = (int)NPC.ai[0];
		FishingMotionOutput motion = FishingEnemyMotion.Step(s.Behavior,
			new FishingMotionInput(tick, NPC.velocity, toTarget, target.velocity, NPC.collideX, NPC.collideY));
		NPC.velocity = motion.Velocity;
		NPC.damage = Math.Max(1, (int)(NPC.defDamage * motion.ContactMultiplier));
		NPC.defense = NPC.defDefense + motion.DefenseBonus;
		NPC.direction = NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;
		NPC.ai[0] = (tick + 1) % 5544000; // common multiple of every behavior cycle, below float's exact-integer limit
		bool wasTelegraph = NPC.ai[1] > .5f;
		NPC.ai[1] = motion.Telegraph ? 1 : 0;
		NPC.ai[2] = motion.ContactMultiplier;
		NPC.ai[3] = motion.DefenseBonus;
		if (motion.ShotCount > 0 && toTarget.LengthSquared() < 800f * 800f
			&& Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
			Shoot(target, motion);
		if (motion.Telegraph != wasTelegraph || motion.ShotCount > 0 || tick % 30 == 0) NPC.netUpdate = true;
	}
	private void Shoot(Player target, in FishingMotionOutput motion)
	{
		Vector2 to = target.Center - NPC.Center;
		if (to.LengthSquared() < .001f) to = Vector2.UnitY;
		Vector2 aim = Vector2.Normalize(to);
		for (int i = 0; i < motion.ShotCount; i++)
		{
			float angle = (i - (motion.ShotCount - 1) * .5f) * .18f;
			Vector2 velocity = aim.RotatedBy(angle) * motion.ShotSpeed;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
				ModContent.ProjectileType<FishingSpineProjectile>(), Math.Max(5, NPC.defDamage / 2),
				1f, Main.myPlayer, motion.DebuffMode);
		}
	}
	public override void FindFrame(int frameHeight)
	{
		int count = Math.Max(1, Main.npcFrameCount[Type]);
		NPC.frameCounter += .15 + NPC.velocity.Length() * .025;
		NPC.frame.Y = ((int)NPC.frameCounter % count) * frameHeight;
	}
	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
	{
		if (Spec.ContactDebuff > 0) target.AddBuff(Spec.ContactDebuff, 90);
	}
	public override void ModifyNPCLoot(NPCLoot loot)
	{
		loot.Add(ItemDropRule.Common(Spec.NativeDrop, 1, 1, 2));
		int material = FishingContentCatalog.GetItemType(Spec.MaterialId);
		if (material > 0) loot.Add(ItemDropRule.Common(material, 2, 1, 2));
	}
	public override Color? GetAlpha(Color drawColor) => NPC.ai[1] > .5f
		? Color.Lerp(drawColor, Color.White, .65f) : null;
}

public sealed class ShoalSnapper : FishingEncounterNpc { protected override int EnemyIndex => 0; }
public sealed class TideBell : FishingEncounterNpc { protected override int EnemyIndex => 1; }
public sealed class ReefCharger : FishingEncounterNpc { protected override int EnemyIndex => 2; }
public sealed class StormSquid : FishingEncounterNpc { protected override int EnemyIndex => 3; }
public sealed class LanternMaw : FishingEncounterNpc { protected override int EnemyIndex => 4; }
public sealed class PressureEel : FishingEncounterNpc { protected override int EnemyIndex => 5; }
public sealed class BlackwaterStalker : FishingEncounterNpc { protected override int EnemyIndex => 6; }
public sealed class WakeCarapace : FishingEncounterNpc { protected override int EnemyIndex => 7; }
public sealed class CrystalUrchin : FishingEncounterNpc { protected override int EnemyIndex => 8; }
public sealed class BlindLakeHunter : FishingEncounterNpc { protected override int EnemyIndex => 9; }
public sealed class RiverJetfish : FishingEncounterNpc { protected override int EnemyIndex => 10; }
public sealed class SiltCrawler : FishingEncounterNpc { protected override int EnemyIndex => 11; }

public sealed class FishingSpineProjectile : ModProjectile
{
	public override string Texture => "Terraria/Images/Projectile_27";
	public override void SetDefaults()
	{
		Projectile.width = Projectile.height = 10;
		Projectile.hostile = true; Projectile.friendly = false;
		Projectile.tileCollide = true; Projectile.ignoreWater = true;
		Projectile.penetrate = 1; Projectile.timeLeft = 180;
	}
	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		Projectile.velocity *= .998f;
	}
	public override Color? GetAlpha(Color lightColor) => (int)Projectile.ai[0] switch
	{
		1 => new Color(95, 178, 208, 200), 2 => new Color(74, 61, 110, 210),
		_ => new Color(189, 175, 241, 220)
	};
	public override void OnHitPlayer(Player target, Player.HurtInfo info)
	{
		int buff = (int)Projectile.ai[0] == 2 ? BuffID.Darkness : (int)Projectile.ai[0] == 1 ? BuffID.Slow : 0;
		if (buff > 0) target.AddBuff(buff, 90);
	}
}
