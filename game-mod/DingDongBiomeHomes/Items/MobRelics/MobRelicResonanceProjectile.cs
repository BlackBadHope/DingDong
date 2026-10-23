using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DingDongBiomeHomes.Items.SynergyArmor;

namespace DingDongBiomeHomes.Items.MobRelics;
public sealed class MobRelicResonanceProjectile : ModProjectile
{
	public override string Texture => "DingDongBiomeHomes/Items/MobRelics/MobRelicResonanceProjectile";

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 10;
	}

	public override void AI()
	{
		int targetIndex = (int)Projectile.ai[0];
		NPC target = targetIndex >= 0 && targetIndex < Main.maxNPCs ? Main.npc[targetIndex] : null;
		if (target != null && target.active && !target.friendly && target.CanBeChasedBy(Projectile))
		{
			Vector2 offset = target.Center - Projectile.Center;
			if (offset.LengthSquared() > 0.001f)
			{
				offset.Normalize();
				int phase = ((int)Projectile.ai[1] >> 8) & 0xFF;
				float wave = (float)Math.Sin((Main.GameUpdateCount + (ulong)phase) * 0.12d) * 0.8f;
				Vector2 perpendicular = new(-offset.Y, offset.X);
				Vector2 desired = offset * 11f + perpendicular * wave;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.12f);
			}
		}

		Projectile.rotation += 0.18f + Projectile.velocity.Length() * 0.01f;
		Color color = UnpackColor((int)Projectile.ai[2]);
		Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.55f);
		if (!Main.dedServ && Main.rand.NextBool(3))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemAmethyst, -Projectile.velocity * 0.08f, 80, color, 0.8f);
			dust.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		MobRelicAspect aspect = (MobRelicAspect)((int)Projectile.ai[1] & 0xFF);
		int buffType = aspect switch
		{
			MobRelicAspect.Cinder => BuffID.OnFire,
			MobRelicAspect.Frost => BuffID.Frostburn,
			MobRelicAspect.Storm => BuffID.Electrified,
			MobRelicAspect.Venom => BuffID.Poisoned,
			MobRelicAspect.Radiant => BuffID.Ichor,
			MobRelicAspect.Umbral => BuffID.ShadowFlame,
			MobRelicAspect.Tidal => BuffID.Wet,
			MobRelicAspect.Verdant => BuffID.Venom,
			_ => BuffID.OnFire
		};
		target.AddBuff(buffType, 90 + (((int)Projectile.ai[1] >> 8) & 0x3F));
	}

	public override Color? GetAlpha(Color lightColor) => UnpackColor((int)Projectile.ai[2]);
	private static Color UnpackColor(int packed) => new((byte)((packed >> 16) & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)(packed & 0xFF), 220);
}
