using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	private static string InferItemDelivery(Item item)
	{
		string className = item.DamageType?.Name ?? string.Empty;
		if (ContainsAny(className, "rogue"))
			return DamageTags.Rogue;
		if (ContainsAny(className, "throw"))
			return DamageTags.Thrown;
		if (item.useAmmo == AmmoID.Arrow)
			return DamageTags.Arrow;
		if (item.useAmmo == AmmoID.Bullet)
			return DamageTags.Bullet;
		if (item.DamageType != null && item.DamageType.CountsAsClass(DamageClass.Summon))
			return DamageTags.MinionProjectile;
		if (item.DamageType != null && item.DamageType.CountsAsClass(DamageClass.Magic) && item.shoot > ProjectileID.None)
			return DamageTags.MagicProjectile;
		return DamageTags.KineticContact;
	}

	private static string InferItemBehavior(Item item)
	{
		if (item.channel)
			return DamageTags.Sustained;
		if (item.useTime > 0 && item.useAnimation >= item.useTime * 3)
			return DamageTags.Burst;
		return DamageTags.Single;
	}

	private static string InferProjectileDelivery(Projectile projectile, DamageLineageGlobalProjectile lineage)
	{
		string identity = StableProjectileIdentity(projectile);
		string className = projectile.DamageType?.Name ?? string.Empty;
		if (projectile.trap)
			return DamageTags.Trap;
		if (projectile.sentry)
			return DamageTags.Sentry;
		if (ProjectileID.Sets.IsAWhip[projectile.type])
			return DamageTags.MinionContact;
		if (projectile.minion)
			return DamageTags.MinionProjectile;
		if (projectile.arrow || lineage?.AmmoItemType == AmmoID.Arrow)
			return DamageTags.Arrow;
		if (lineage?.AmmoItemType == AmmoID.Bullet)
			return DamageTags.Bullet;
		if (ContainsAny(identity, "explosion", "blast", "bomb", "grenade", "rocket", "dynamite"))
			return DamageTags.Explosion;
		if (ContainsAny(identity, "beam", "laser", "ray"))
			return DamageTags.Beam;
		if (ContainsAny(className, "rogue"))
			return DamageTags.Rogue;
		if (ContainsAny(className, "throw"))
			return DamageTags.Thrown;
		if (projectile.DamageType != null && projectile.DamageType.CountsAsClass(DamageClass.Magic))
			return DamageTags.MagicProjectile;
		return DamageTags.KineticContact;
	}

	private static string InferProjectileBehavior(Projectile projectile)
	{
		string identity = StableProjectileIdentity(projectile);
		if ((projectile.usesLocalNPCImmunity && projectile.localNPCHitCooldown > 0) || (projectile.usesIDStaticNPCImmunity && projectile.idStaticNPCHitCooldown > 0))
			return DamageTags.RapidMultihit;
		if (projectile.minion || projectile.sentry)
			return DamageTags.Sustained;
		if (ContainsAny(identity, "explosion", "blast", "bomb", "grenade", "nova"))
			return DamageTags.Area;
		if (projectile.penetrate < 0 || projectile.penetrate > 1)
			return DamageTags.Piercing;
		if (ContainsAny(identity, "beam", "laser", "ray"))
			return DamageTags.Sustained;
		return DamageTags.Single;
	}
}
