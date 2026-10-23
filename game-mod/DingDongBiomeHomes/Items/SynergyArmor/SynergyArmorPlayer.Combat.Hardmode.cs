using System;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	private void ReleaseStormChain(NPC target, int damage)
	{
		if (stormCharge < 100f)
			return;
		foreach (NPC chainTarget in NearbyHostiles(target.Center, 640f, 3))
			SpawnPulse(chainTarget, Scale(damage, 0.40f), SynergyArmorPulseMode.Storm);
		stormCharge = 0f;
	}

	private void SettleMimicCredit(NPC target, int damage, bool critical)
	{
		int creditLimit = tierCounts[(int)MobRelicTier.CommonCurio] >= 2 ? 8 : 5;
		if (critical && primaryCounter > 0)
		{
			SpawnPulse(target, Scale(damage, 0.18f * primaryCounter), SynergyArmorPulseMode.Broker);
			Heal(primaryCounter);
			Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + primaryCounter * 3);
			primaryCounter = 0;
		}
		else
		{
			primaryCounter = Math.Min(creditLimit, primaryCounter + 1);
		}
	}

	private void RelayFrostHit(NPC target, int damage)
	{
		if (trackedTarget == target.whoAmI && cooldown <= 0)
		{
			float factor = HasAspect(MobRelicAspect.Frost) ? 0.65f : 0.45f;
			SpawnPulse(target, Scale(damage, factor), SynergyArmorPulseMode.Frost, delay: 30);
			cooldown = 45;
		}
		trackedTarget = target.whoAmI;
	}

	private void StoreRootclockDamage(NPC target, int damage)
	{
		TrackTarget(target, clearStoredDamage: true);
		storedDamage = Math.Min(5000, storedDamage + damage);
		delayedTarget = target.whoAmI;
		delayedTimer = HasAspect(MobRelicAspect.Verdant) || HasAspect(MobRelicAspect.Tidal) ? 120 : 180;
	}

	private void AccelerateParadox()
	{
		if (storedDamage > 0)
			delayedTimer = Math.Max(30, delayedTimer - 15);
	}
}
