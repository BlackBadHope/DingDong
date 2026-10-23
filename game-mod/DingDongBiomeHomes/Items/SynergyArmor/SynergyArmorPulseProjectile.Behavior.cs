using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed partial class SynergyArmorPulseProjectile
{
	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 180;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 12;
	}

	public override void AI()
	{
		if (Projectile.ai[2] > 0f)
		{
			Projectile.ai[2]--;
			Projectile.friendly = false;
			Projectile.velocity *= 0.90f;
			Projectile.alpha = 150;
			Projectile.rotation -= 0.12f;
			return;
		}

		Projectile.friendly = true;
		Projectile.alpha = 0;
		NPC target = Target();
		if (target != null && target.CanBeChasedBy(Projectile))
		{
			Vector2 offset = target.Center - Projectile.Center;
			if (offset.LengthSquared() > 0.001f)
			{
				offset.Normalize();
				float turn = Mode is SynergyArmorPulseMode.Prism or SynergyArmorPulseMode.Lunar ? 0.08f : 0.16f;
				float speed = Mode is SynergyArmorPulseMode.Burrow or SynergyArmorPulseMode.Horizon ? 7f : 11f;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, offset * speed, turn);
			}
		}

		if (Mode is SynergyArmorPulseMode.Burrow or SynergyArmorPulseMode.Bell or SynergyArmorPulseMode.Paradox or SynergyArmorPulseMode.Horizon or SynergyArmorPulseMode.Architect)
		{
			Projectile.scale = Math.Min(1.8f, Projectile.scale + 0.012f);
		}

		Projectile.rotation += 0.16f + Projectile.velocity.Length() * 0.008f;
		Color color = ModeColor(Mode);
		Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.5f);
		if (!Main.dedServ && Main.rand.NextBool(3))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemAmethyst, -Projectile.velocity * 0.06f, 80, color, 0.8f);
			dust.noGravity = true;
		}
	}

	public override Color? GetAlpha(Color lightColor) => ModeColor(Mode) * Projectile.Opacity;
	private NPC Target()
	{
		int targetIndex = (int)Projectile.ai[1];
		return targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex]?.active == true ? Main.npc[targetIndex] : null;
	}
}
