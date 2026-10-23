using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal static partial class MobRelicAudit
{
	private static void AuditResonanceProjectile(MobRelicAuditContext context)
	{
		try
		{
			int type = ModContent.ProjectileType<MobRelicResonanceProjectile>();
			if (ContentSamples.ProjectilesByType.TryGetValue(type, out Projectile projectile) && projectile.ModProjectile is MobRelicResonanceProjectile && projectile.friendly && !projectile.hostile && projectile.penetrate == 1 && projectile.DamageType == DamageClass.Generic)
				context.ResonanceProjectilePassed = 1;
			else
				Failure(context, "resonance-projectile");
		}
		catch (Exception exception)
		{
			Failure(context, $"resonance-projectile:{exception.GetType().Name}");
		}
	}
}
