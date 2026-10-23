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
	private int TryReleaseEcho(NPC target, int damageDone, bool force = false)
	{
		if (!hasEcho || echoCooldown > 0 || damageDone <= 0 || target == null || !target.active || target.friendly || Player.whoAmI != Main.myPlayer)
		{
			return -1;
		}

		float chance = Math.Max(echoAnchor.EchoChance, 0.08f) + 0.04f * (echoSetSize - MobRelicMechanics.EchoThreshold) + (fullConvergence ? 0.15f : 0f);
		chance = Math.Min(chance, 0.70f);
		if (!force && Main.rand.NextFloat() > chance)
			return -1;
		float factor = Math.Max(echoAnchor.EchoDamageFactor, 0.22f) + 0.04f * (echoSetSize - MobRelicMechanics.EchoThreshold) + (fullConvergence ? 0.12f : 0f);
		int echoDamage = Math.Max(1, (int)Math.Round(damageDone * factor, MidpointRounding.AwayFromZero));
		Vector2 direction = target.Center - Player.Center;
		if (direction.LengthSquared() < 0.001f)
			direction = Vector2.UnitY;
		direction.Normalize();
		int aspectAndPhase = (int)echoAnchor.Aspect | (echoAnchor.MotionPhase << 8);
		int projectileIndex = Projectile.NewProjectile(Player.GetSource_FromThis("DingDongMobRelicResonance"), Player.Center, direction * 7f, ModContent.ProjectileType<MobRelicResonanceProjectile>(), echoDamage, 2f, Player.whoAmI, target.whoAmI, aspectAndPhase, echoAnchor.PackedColor);
		echoCooldown = Math.Max(12, echoAnchor.EchoCooldownTicks - (echoSetSize - 3) * 6 - (fullConvergence ? 6 : 0));
		return projectileIndex;
	}
}
