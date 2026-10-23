using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	private void TrackTarget(NPC target, bool clearStoredDamage = false)
	{
		if (trackedTarget == target.whoAmI)
			return;
		trackedTarget = target.whoAmI;
		primaryCounter = 0;
		if (clearStoredDamage)
			storedDamage = 0;
	}

	private int SpawnPulse(NPC target, int damage, SynergyArmorPulseMode mode, int delay = 0, float spread = 0f)
	{
		if (target == null || !target.active || target.friendly || damage <= 0 || Player.whoAmI != Main.myPlayer)
			return -1;
		Vector2 direction = target.Center - Player.Center;
		if (direction.LengthSquared() < 0.001f)
			direction = Vector2.UnitY;
		direction.Normalize();
		direction = direction.RotatedBy(spread);
		return Projectile.NewProjectile(Player.GetSource_FromThis($"DingDongSynergyArmor:{mode}"), Player.Center, direction * 8f, ModContent.ProjectileType<SynergyArmorPulseProjectile>(), damage, 2f, Player.whoAmI, (float)mode, target.whoAmI, delay);
	}

	private static IEnumerable<NPC> NearbyHostiles(Vector2 center, float radius, int limit) => Main.npc.Where(npc => npc != null && npc.active && !npc.friendly && npc.CanBeChasedBy() && Vector2.DistanceSquared(npc.Center, center) <= radius * radius).OrderBy(npc => Vector2.DistanceSquared(npc.Center, center)).Take(limit);
	private static NPC ClosestHostile(Vector2 center, float radius) => NearbyHostiles(center, radius, 1).FirstOrDefault();

	private static void PullHostiles(Vector2 center, float radius)
	{
		foreach (NPC npc in NearbyHostiles(center, radius, 12))
		{
			Vector2 pull = center - npc.Center;
			if (pull.LengthSquared() <= 1f)
				continue;
			pull.Normalize();
			npc.velocity = Vector2.Lerp(npc.velocity, pull * 8f, 0.35f);
			npc.netUpdate = true;
		}
	}

	private void Heal(int amount)
	{
		if (amount <= 0 || Player.statLife <= 0)
			return;
		int before = Player.statLife;
		Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + amount);
		int healed = Player.statLife - before;
		if (healed > 0 && !Main.dedServ)
			Player.HealEffect(healed);
	}

	private static int Scale(int damage, float factor) => Math.Max(1, (int)Math.Round(damage * factor, MidpointRounding.AwayFromZero));

	private static int DamageClassMask(DamageClass damageClass)
	{
		int mask = 0;
		if (damageClass.CountsAsClass(DamageClass.Melee)) mask |= 1;
		if (damageClass.CountsAsClass(DamageClass.Ranged)) mask |= 2;
		if (damageClass.CountsAsClass(DamageClass.Magic)) mask |= 4;
		if (damageClass.CountsAsClass(DamageClass.Summon)) mask |= 8;
		return mask;
	}

	private static int BitCount(int value)
	{
		int count = 0;
		while (value != 0)
		{
			count += value & 1;
			value >>= 1;
		}
		return count;
	}
}
