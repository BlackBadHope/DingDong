using System;
using DingDongBiomeHomes.Items.MobRelics;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	public override void PostUpdate()
	{
		if (cooldown > 0)
			cooldown--;
		if (temporaryWardTicks > 0)
			temporaryWardTicks--;
		if (commissionTicks > 0)
			commissionTicks--;
		if (activeSet == null)
		{
			ResetTransientState();
			previousCenter = Player.Center;
			return;
		}

		float distance = previousCenter == Vector2.Zero ? 0f : Vector2.Distance(previousCenter, Player.Center);
		previousCenter = Player.Center;
		switch (activeSet.Id)
		{
			case SynergyArmorSetId.ThresholdForager:
				travelCharge = Math.Min(120f, travelCharge + distance / 16f * (HasAspect(MobRelicAspect.Verdant) ? 1.5f : 1f));
				break;
			case SynergyArmorSetId.BurrowBell:
				if (Player.velocity.LengthSquared() < 0.08f || HasAspect(MobRelicAspect.Tidal) && Player.wet)
					stillnessTicks = Math.Min(180, stillnessTicks + 1);
				else if (!HasAspect(MobRelicAspect.Tidal))
					stillnessTicks = Math.Max(0, stillnessTicks - 3);
				break;
			case SynergyArmorSetId.StormConductor:
				float verticalWeight = HasAspect(MobRelicAspect.Storm) ? 1f : 0.45f;
				stormCharge = Math.Min(120f, stormCharge + Math.Abs(Player.velocity.X) * 0.32f + Math.Abs(Player.velocity.Y) * verticalWeight * 0.25f);
				break;
		}

		if (delayedTimer > 0 && --delayedTimer == 0)
			ResolveDelayedEffect();
	}

	private void ResolveDelayedEffect()
	{
		NPC target = delayedTarget >= 0 && delayedTarget < Main.maxNPCs ? Main.npc[delayedTarget] : null;
		if (activeSet?.Id == SynergyArmorSetId.RootclockGardener && target?.active == true && storedDamage > 0)
		{
			float factor = HasAspect(MobRelicAspect.Verdant) || HasAspect(MobRelicAspect.Tidal) ? 0.48f : 0.35f;
			SpawnPulse(target, Scale(storedDamage, factor), SynergyArmorPulseMode.Bloom);
			Heal(Math.Max(1, Scale(storedDamage, factor * 0.08f)));
		}
		else if (activeSet?.Id == SynergyArmorSetId.TempleParadox && storedDamage > 0)
		{
			Heal(storedDamage);
			if (target?.active == true)
				SpawnPulse(target, storedDamage, SynergyArmorPulseMode.Paradox);
		}

		storedDamage = 0;
		delayedTarget = -1;
	}
}
