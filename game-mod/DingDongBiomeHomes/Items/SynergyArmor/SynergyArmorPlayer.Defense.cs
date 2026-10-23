using System;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	public override void PostHurt(Player.HurtInfo info)
	{
		if (activeSet == null || info.Damage <= 0)
			return;
		NPC target = ClosestHostile(Player.Center, 720f);
		switch (activeSet.Id)
		{
			case SynergyArmorSetId.CinderBellfounder when cooldown <= 0 && target != null:
				SpawnPulse(target, Math.Max(1, (int)Math.Round(info.Damage * 1.4f)), SynergyArmorPulseMode.Bell);
				cooldown = HasAspect(MobRelicAspect.Cinder) ? 150 : 240;
				break;
			case SynergyArmorSetId.TempleParadox:
				float debtFactor = HasAspect(MobRelicAspect.Radiant) && HasAspect(MobRelicAspect.Umbral) ? 0.70f : 0.45f;
				storedDamage = Math.Min(300, storedDamage + Math.Max(1, (int)Math.Round(info.Damage * debtFactor)));
				delayedTimer = 180;
				delayedTarget = target?.whoAmI ?? -1;
				break;
			case SynergyArmorSetId.MoonlordThreshold when relicWardEnabled && cooldown <= 0 && target != null:
				float reflect = relicConvergenceEnabled ? 1.2f : 0.8f;
				SpawnPulse(target, Math.Max(1, (int)Math.Round(info.Damage * reflect)), SynergyArmorPulseMode.Threshold);
				cooldown = 60;
				break;
			case SynergyArmorSetId.LegendaryBellArchitect when ArchitectPhase() == 1:
				Heal(Math.Max(1, info.Damage / 5));
				break;
		}
	}

	private void ApplyArchitectPhase()
	{
		bool overlap = providers.Count >= 3 && relicConvergenceEnabled;
		int phase = ArchitectPhase();
		if (overlap || phase == 0)
		{
			Player.GetDamage(DamageClass.Generic) += 0.18f;
			Player.GetCritChance(DamageClass.Generic) += 8f;
		}
		if (overlap || phase == 1)
		{
			Player.statDefense += 14;
			Player.endurance += 0.08f;
			Player.lifeRegen += 4;
			Player.noKnockback = true;
		}
		if (overlap || phase == 2)
		{
			Player.moveSpeed += 0.18f;
			Player.jumpSpeedBoost += 1.5f;
			Player.maxRunSpeed += 1f;
		}
	}

	private int ArchitectPhase() => (int)(Main.GameUpdateCount / 300UL % 3UL);
}
