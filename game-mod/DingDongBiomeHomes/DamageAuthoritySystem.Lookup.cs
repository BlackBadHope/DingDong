using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Applies deterministic client prediction and validates the same decision on
/// the server before the vanilla DamageNPC packet is allowed to mutate life.
/// </summary>
public sealed partial class DamageAuthoritySystem
{
	private static Projectile FindProjectile(int owner, int identity, int type)
	{
		for (int index = 0; index < Main.maxProjectiles; index++)
		{
			Projectile projectile = Main.projectile[index];
			if (projectile.active && projectile.owner == owner && projectile.identity == identity && projectile.type == type)
				return projectile;
		}

		return null;
	}

	private static bool TryCreateItemPayloadEnvelope(Player player, NPC target, Item item, float multiplier, out DamagePayloadEnvelope envelope)
	{
		envelope = default;
		if (!float.IsFinite(multiplier) || multiplier <= 0f || item.damage <= 0 || item.noMelee || target.dontTakeDamage || target.immortal || !player.CanNPCBeHitByPlayerOrPlayerProjectile(target, null))
			return false;
		bool? canHit = CombinedHooks.CanPlayerHitNPCWithItem(player, item, target);
		if (canHit == false || canHit != true && target.friendly && !CanDamageFriendly(player, target, item))
			return false;
		if (target.immune[player.whoAmI] > 0 || !player.CanHitNPCWithMeleeHit(target.whoAmI) || player.attackCD > 0)
			return false;
		float scale = MathF.Max(1f, player.GetAdjustedItemScale(item));
		int reach = Math.Clamp((int)MathF.Ceiling(Math.Max(item.width, item.height) * scale * 2f) + 64, 96, 256);
		Rectangle hitbox = player.Hitbox;
		hitbox.Inflate(reach, reach);
		bool? customCollision = CombinedHooks.CanPlayerMeleeAttackCollideWithNPC(player, item, hitbox, target);
		if (customCollision == false || customCollision != true && !hitbox.Intersects(target.Hitbox))
			return false;
		if (!target.noTileCollide && !player.CanHit(target))
			return false;

		NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(item.DamageType, player.direction, false);
		player.ApplyBannerOffenseBuff(target, ref modifiers);
		ApplyVanillaItemPayloadModifiers(player, target, item, ref modifiers);
		modifiers.ArmorPenetration += player.GetWeaponArmorPenetration(item);
		CombinedHooks.ModifyPlayerHitNPCWithItem(player, item, target, ref modifiers);
		modifiers.FinalDamage *= multiplier;
		bool mayCrit = item.DamageType.UseStandardCritCalcs && player.GetWeaponCrit(item) > 0;
		return DamagePayloadAuthority.TryBuild(item.DamageType, player.GetWeaponDamage(item), player.GetWeaponKnockback(item), mayCrit, modifiers, out envelope);
	}

	private static bool TryCreateProjectilePayloadEnvelope(Player player, NPC target, Projectile projectile, float multiplier, out DamagePayloadEnvelope envelope)
	{
		envelope = default;
		if (!float.IsFinite(multiplier) || multiplier <= 0f || projectile == null || !projectile.active || projectile.owner != player.whoAmI || !projectile.friendly || projectile.hostile || projectile.damage <= 0 || projectile.penetrate == 0 || target.dontTakeDamage || target.immortal)
			return false;
		if (!player.CanNPCBeHitByPlayerOrPlayerProjectile(target, projectile))
			return false;
		bool? canHit = CombinedHooks.CanHitNPCWithProj(projectile, target);
		if (canHit == false || canHit != true && target.friendly)
			return false;
		if (!projectile.usesLocalNPCImmunity && !projectile.usesIDStaticNPCImmunity && target.immune[player.whoAmI] > 0)
			return false;
		if (projectile.usesLocalNPCImmunity && projectile.localNPCImmunity[target.whoAmI] > 0)
			return false;
		Rectangle hitbox = projectile.Hitbox;
		int lag = Math.Clamp((int)MathF.Ceiling(projectile.velocity.Length() * 3f) + 8, 8, 64);
		hitbox.Inflate(lag, lag);
		if (!projectile.Colliding(hitbox, target.Hitbox) || projectile.ownerHitCheck && !player.CanHit(target))
			return false;

		NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(projectile.DamageType, projectile.direction, false);
		modifiers.ArmorPenetration += projectile.ArmorPenetration;
		CombinedHooks.ModifyHitNPCWithProj(projectile, target, ref modifiers);
		player.ApplyBannerOffenseBuff(target, ref modifiers);
		modifiers.FinalDamage *= multiplier;
		bool mayCrit = projectile.DamageType.UseStandardCritCalcs && projectile.CritChance > 0;
		return DamagePayloadAuthority.TryBuild(projectile.DamageType, projectile.damage, projectile.knockBack, mayCrit, modifiers, out envelope);
	}
}
