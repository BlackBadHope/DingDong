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
public sealed partial class MobRelicPlayer
{
	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		TryReleaseEcho(target, damageDone);
		TryRunPersonalMechanics(target, hit, damageDone, DamageMask(item.DamageType));
	}

	public override void OnHitNPCWithProj(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (projectile.ModProjectile is MobRelicResonanceProjectile)
			return;
		TryReleaseEcho(target, damageDone);
		TryRunPersonalMechanics(target, hit, damageDone, DamageMask(projectile.DamageType));
	}

	public override void PostHurt(Player.HurtInfo info)
	{
		if (info.Damage > 0)
			afterHurtTicks = 180;
		if (!hasWard || wardCooldown > 0)
			return;
		wardActiveTicks = Math.Max(wardAnchor.WardDurationTicks, 180) + (fullConvergence ? 120 : 0);
		wardCooldown = Math.Max(420, 660 - wardSetSize * 45 - wardAnchor.MotionPhase % 90);
		if (!Main.dedServ)
			CombatText.NewText(Player.Hitbox, wardAnchor.ResonanceColor, "Threshold Ward");
	}

	private static int DamageMask(DamageClass damageClass)
	{
		if (damageClass == null)
			return 0;
		int mask = 0;
		if (damageClass.CountsAsClass(DamageClass.Melee))
			mask |= 1;
		if (damageClass.CountsAsClass(DamageClass.Ranged))
			mask |= 2;
		if (damageClass.CountsAsClass(DamageClass.Magic))
			mask |= 4;
		if (damageClass.CountsAsClass(DamageClass.Summon))
			mask |= 8;
		return mask;
	}
}
