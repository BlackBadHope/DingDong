using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed class AccessoryEchoProjectile : ModProjectile
{
	public override string Texture => "DingDongBiomeHomes/Assets/AccessoryEnchanting/EchoProjectile";

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 90;
		Projectile.DamageType = DamageClass.Generic;
	}

	public override void AI()
	{
		int targetIndex = (int)Projectile.ai[0];
		if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].active || Main.npc[targetIndex].friendly)
		{
			Projectile.Kill();
			return;
		}

		Microsoft.Xna.Framework.Vector2 direction = Main.npc[targetIndex].Center - Projectile.Center;
		if (direction.LengthSquared() > 0.01f)
		{
			direction.Normalize();
			Projectile.velocity = Microsoft.Xna.Framework.Vector2.Lerp(Projectile.velocity, direction * 12f, 0.20f);
		}

		Projectile.rotation += 0.25f;
	}
}
