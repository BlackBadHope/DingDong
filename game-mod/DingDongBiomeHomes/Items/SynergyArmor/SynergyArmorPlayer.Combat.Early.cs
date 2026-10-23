using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	private void ReleaseTravelBriar(NPC target, int damage)
	{
		if (travelCharge < 90f)
			return;
		SpawnPulse(target, Scale(damage, 0.45f), SynergyArmorPulseMode.Briar);
		Heal(HasAspect(MobRelicAspect.Verdant) ? 6 : 3);
		travelCharge = 0f;
	}

	private void ReleasePrismVolley(NPC target, int damage, bool critical)
	{
		if (!critical || ++primaryCounter < 3)
			return;
		float factor = HasAspect(MobRelicAspect.Radiant) ? 0.40f : 0.30f;
		for (int i = 0; i < 3; i++)
			SpawnPulse(target, Scale(damage, factor), SynergyArmorPulseMode.Prism, spread: (i - 1) * 0.18f);
		primaryCounter = 0;
	}

	private void ReleaseBurrowBell(NPC target, int damage)
	{
		if (stillnessTicks < 120)
			return;
		SpawnPulse(target, Scale(damage, 0.65f), SynergyArmorPulseMode.Burrow);
		temporaryWardTicks = 120;
		stillnessTicks = 0;
	}

	private void ConductHiveVolley(NPC target, int damage)
	{
		TrackTarget(target);
		if (++primaryCounter < 4)
			return;
		for (int i = 0; i < 4; i++)
			SpawnPulse(target, Scale(damage, 0.22f), SynergyArmorPulseMode.Hive, spread: (i - 1.5f) * 0.15f);
		primaryCounter = 0;
	}

	private void DrawDamageRoute(NPC target, int damage, DamageClass damageClass)
	{
		classMask |= DamageClassMask(damageClass);
		int required = providers.Count >= 3 ? 2 : 3;
		if (BitCount(classMask) < required)
			return;
		SpawnPulse(target, Scale(damage, providers.Count >= 3 ? 0.80f : 0.60f), SynergyArmorPulseMode.Route);
		classMask = 0;
	}
}
