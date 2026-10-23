using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	private void AccumulateHorizonMass(NPC target, int damage)
	{
		int threshold = HasAspect(MobRelicAspect.Umbral) ? 5 : 6;
		if (++primaryCounter < threshold)
			return;
		PullHostiles(target.Center, HasAspect(MobRelicAspect.Umbral) ? 760f : 560f);
		SpawnPulse(target, Scale(damage, 0.85f), SynergyArmorPulseMode.Horizon);
		primaryCounter = 0;
	}

	private void AdvanceLunarCommission(NPC target, int damage, DamageClass damageClass)
	{
		classMask |= DamageClassMask(damageClass);
		if ((classMask & 0xF) != 0xF)
			return;
		commissionTicks = providers.Count >= 4 ? 900 : 600;
		for (int i = 0; i < 4; i++)
			SpawnPulse(target, Scale(damage, 0.28f), SynergyArmorPulseMode.Lunar, spread: (i - 1.5f) * 0.2f);
		classMask = 0;
	}

	private void ReleaseThresholdEchoes(NPC target, int damage)
	{
		if (!relicEchoEnabled || ++primaryCounter < 3)
			return;
		float factor = relicConvergenceEnabled ? 0.32f : 0.22f;
		int echoes = relicConvergenceEnabled ? 3 : 2;
		for (int i = 0; i < echoes; i++)
			SpawnPulse(target, Scale(damage, factor), SynergyArmorPulseMode.Threshold, delay: 18 + i * 12, spread: (i - 1) * 0.12f);
		primaryCounter = 0;
	}

	private void StrikeArchitectBell(NPC target, int damage)
	{
		bool overlap = providers.Count >= 3 && relicConvergenceEnabled;
		if (++primaryCounter < (overlap ? 3 : 5))
			return;
		SpawnPulse(target, Scale(damage, overlap ? 1.25f : 0.75f), SynergyArmorPulseMode.Architect);
		primaryCounter = 0;
	}
}
