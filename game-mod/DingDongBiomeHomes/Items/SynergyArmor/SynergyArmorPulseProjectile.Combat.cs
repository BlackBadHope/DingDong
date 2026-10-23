using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public sealed partial class SynergyArmorPulseProjectile
{
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		switch (Mode)
		{
			case SynergyArmorPulseMode.Briar:
				target.AddBuff(BuffID.Poisoned, 180);
				target.AddBuff(BuffID.Slow, 90);
				break;
			case SynergyArmorPulseMode.Prism:
				target.AddBuff(BuffID.Ichor, 120);
				break;
			case SynergyArmorPulseMode.Burrow:
				target.AddBuff(BuffID.Slow, 120);
				break;
			case SynergyArmorPulseMode.Hive:
				target.AddBuff(BuffID.Venom, 150);
				break;
			case SynergyArmorPulseMode.Route:
				target.AddBuff(BuffID.Confused, 90);
				break;
			case SynergyArmorPulseMode.Bell:
				target.AddBuff(BuffID.OnFire3, 240);
				break;
			case SynergyArmorPulseMode.Storm:
				target.AddBuff(BuffID.Electrified, 150);
				break;
			case SynergyArmorPulseMode.Broker:
				target.AddBuff(BuffID.Midas, 300);
				break;
			case SynergyArmorPulseMode.Frost:
				target.AddBuff(BuffID.Frostburn2, 180);
				break;
			case SynergyArmorPulseMode.Bloom:
				target.AddBuff(BuffID.Venom, 240);
				break;
			case SynergyArmorPulseMode.Paradox:
				target.AddBuff(BuffID.Ichor, 120);
				target.AddBuff(BuffID.ShadowFlame, 120);
				break;
			case SynergyArmorPulseMode.Horizon:
				target.AddBuff(BuffID.ShadowFlame, 210);
				break;
			case SynergyArmorPulseMode.Lunar:
				target.AddBuff((Projectile.identity & 3) switch
				{
					0 => BuffID.Daybreak,
					1 => BuffID.Electrified,
					2 => BuffID.Ichor,
					_ => BuffID.Venom
				}, 150);
				break;
			case SynergyArmorPulseMode.Threshold:
				target.AddBuff(BuffID.ShadowFlame, 180);
				break;
			case SynergyArmorPulseMode.Architect:
				target.AddBuff(BuffID.OnFire3, 180);
				target.AddBuff(BuffID.Ichor, 180);
				target.AddBuff(BuffID.Electrified, 180);
				break;
		}
	}
}
