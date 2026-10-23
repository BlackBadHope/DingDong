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
	public static DamageSourceClassification Classify(Item item)
	{
		ArgumentNullException.ThrowIfNull(item);
		(string acquisition, string element) = DamageAffinityGlobalItem.DefaultsFor(item);
		if (item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity))
		{
			acquisition = affinity.AcquisitionTag;
			element = affinity.ElementTag;
		}

		string delivery = InferItemDelivery(item);
		string behavior = InferItemBehavior(item);
		DamageCoverageKind coverage = item.type == ItemID.SlimeStaff || item.type == ItemID.BabyBirdStaff ? DamageCoverageKind.Explicit : item.ModItem != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		return new DamageSourceClassification(CreateSource(ItemKey(item), delivery, element, behavior, acquisition), coverage, false);
	}

	public static DamageSourceClassification Classify(Projectile projectile)
	{
		ArgumentNullException.ThrowIfNull(projectile);
		projectile.TryGetGlobalProjectile(out DamageLineageGlobalProjectile lineage);
		string delivery = InferProjectileDelivery(projectile, lineage);
		string behavior = InferProjectileBehavior(projectile);
		string element = DamageTagWire.SanitizeElement(lineage?.ElementTag, InferElement(StableProjectileIdentity(projectile)));
		string acquisition = DamageTagWire.SanitizeAcquisition(lineage?.AcquisitionTag, DamageTags.UnspecifiedAcquisition);
		DamageCoverageKind coverage = lineage?.AttackInstanceId != 0 ? DamageCoverageKind.Inherited : projectile.ModProjectile != null ? DamageCoverageKind.Adapter : DamageCoverageKind.Fallback;
		bool trueMultistrike = (projectile.usesLocalNPCImmunity && projectile.localNPCHitCooldown > 0) || (projectile.usesIDStaticNPCImmunity && projectile.idStaticNPCHitCooldown > 0);
		return new DamageSourceClassification(CreateSource(ProjectileKey(projectile), delivery, element, behavior, acquisition), coverage, trueMultistrike);
	}
}
