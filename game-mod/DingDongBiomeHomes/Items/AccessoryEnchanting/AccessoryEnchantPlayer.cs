using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public sealed class AccessoryEnchantPlayer : ModPlayer
{
	private readonly int[] counts = new int[AccessoryEnchantmentCatalog.EnchantmentCount + 1];
	private int hitCounter;
	private int secondWindTicks;
	public override void ResetEffects() => Array.Clear(counts);
	public override void UpdateDead()
	{
		hitCounter = 0;
		secondWindTicks = 0;
	}

	internal void Apply(AccessoryEnchantmentId enchantment)
	{
		if (AccessoryEnchantmentCatalog.IsValid(enchantment))
			counts[(int)enchantment]++;
	}

	internal int Count(AccessoryEnchantmentId enchantment) => counts[(int)enchantment];
	public override void PostUpdateEquips()
	{
		Player.statDefense += 3 * Count(AccessoryEnchantmentId.Bastion);
		Player.GetDamage(DamageClass.Generic) += 0.03f * Count(AccessoryEnchantmentId.Menace);
		Player.GetCritChance(DamageClass.Generic) += 3f * Count(AccessoryEnchantmentId.Precision);
		Player.moveSpeed += 0.04f * Count(AccessoryEnchantmentId.Velocity);
		Player.GetAttackSpeed(DamageClass.Generic) += 0.03f * Count(AccessoryEnchantmentId.Rhythm);
		Player.statLifeMax2 += 20 * Count(AccessoryEnchantmentId.Vitality);
		Player.statManaMax2 += 20 * Count(AccessoryEnchantmentId.Arcana);
		Player.manaCost -= Math.Min(0.30f, 0.03f * Count(AccessoryEnchantmentId.Arcana));
		Player.GetDamage(DamageClass.Melee) += 0.05f * Count(AccessoryEnchantmentId.Vanguard);
		Player.GetDamage(DamageClass.Ranged) += 0.05f * Count(AccessoryEnchantmentId.Ballistics);
		Player.GetDamage(DamageClass.Magic) += 0.05f * Count(AccessoryEnchantmentId.Spellweave);
		Player.GetDamage(DamageClass.Summon) += 0.05f * Count(AccessoryEnchantmentId.Command);
		Player.luck += Math.Min(0.60f, 0.12f * Count(AccessoryEnchantmentId.Fortune));
		if (Count(AccessoryEnchantmentId.Bastion) > 0 && Count(AccessoryEnchantmentId.Vitality) > 0)
		{
			Player.statDefense += 4;
			Player.statLifeMax2 += 20;
		}

		if (Count(AccessoryEnchantmentId.Precision) > 0 && Count(AccessoryEnchantmentId.Rhythm) > 0)
		{
			Player.GetCritChance(DamageClass.Generic) += 2f;
			Player.GetAttackSpeed(DamageClass.Generic) += 0.03f;
		}

		if (Count(AccessoryEnchantmentId.Velocity) > 0 && Count(AccessoryEnchantmentId.Fortune) > 0 && Player.velocity.LengthSquared() >= 36f)
			Player.luck += 0.08f;
		if (Count(AccessoryEnchantmentId.Vanguard) > 0 && Count(AccessoryEnchantmentId.Ballistics) > 0 && Count(AccessoryEnchantmentId.Spellweave) > 0 && Count(AccessoryEnchantmentId.Command) > 0)
			Player.GetDamage(DamageClass.Generic) += 0.05f;
		if (secondWindTicks > 0)
		{
			secondWindTicks--;
			Player.statDefense += 6 + (Count(AccessoryEnchantmentId.Bastion) > 0 ? 6 : 0);
			Player.lifeRegen += 4 + 2 * Count(AccessoryEnchantmentId.SecondWind);
		}
	}

	public override void PostHurt(Player.HurtInfo info)
	{
		if (info.Damage > 0 && Count(AccessoryEnchantmentId.SecondWind) > 0)
			secondWindTicks = Math.Min(600, 180 + 60 * Count(AccessoryEnchantmentId.SecondWind));
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) => HandleEcho(target, damageDone);
	public override void OnHitNPCWithProj(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (projectile.ModProjectile is not AccessoryEchoProjectile)
			HandleEcho(target, damageDone);
	}

	private void HandleEcho(NPC target, int damageDone)
	{
		int echoes = Count(AccessoryEnchantmentId.Echo);
		if (echoes <= 0 || damageDone <= 0 || target == null || !target.active || target.friendly || Player.whoAmI != Main.myPlayer)
			return;
		if (++hitCounter < 5)
			return;
		hitCounter = 0;
		float factor = Math.Min(0.40f, 0.20f + 0.05f * (echoes - 1));
		Projectile.NewProjectile(Player.GetSource_FromThis("DingDongAccessoryEcho"), Player.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<AccessoryEchoProjectile>(), Math.Max(1, (int)Math.Round(damageDone * factor)), 1f, Player.whoAmI, target.whoAmI);
	}
}
