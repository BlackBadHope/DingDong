using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	public override void PostUpdateEquips()
	{
		if (activeSet == null)
			return;
		MobRelicPlayer relicPlayer = Player.GetModPlayer<MobRelicPlayer>();
		relicEchoEnabled = relicPlayer.EchoEnabled;
		relicWardEnabled = relicPlayer.WardEnabled;
		relicConvergenceEnabled = relicPlayer.FullConvergenceEnabled;
		switch (activeSet.Id)
		{
			case SynergyArmorSetId.ThresholdForager:
				Player.moveSpeed += 0.04f;
				Player.lifeRegen += HasAspect(MobRelicAspect.Verdant) ? 3 : 1;
				break;
			case SynergyArmorSetId.PrismGlasswright:
				Player.GetCritChance(DamageClass.Generic) += HasAspect(MobRelicAspect.Radiant) ? 12f : 8f;
				break;
			case SynergyArmorSetId.BurrowBell:
				Player.statDefense += 6;
				if (temporaryWardTicks > 0)
				{
					Player.statDefense += 8;
					Player.noKnockback = true;
				}
				break;
			case SynergyArmorSetId.HiveCantor:
				Player.maxMinions += 1;
				Player.GetDamage(DamageClass.Summon) += HasAspect(MobRelicAspect.Venom) || HasAspect(MobRelicAspect.Verdant) ? 0.10f : 0.05f;
				break;
			case SynergyArmorSetId.DungeonCartographer:
				Player.GetAttackSpeed(DamageClass.Generic) += providers.Count >= 3 ? 0.12f : 0.08f;
				break;
			case SynergyArmorSetId.CinderBellfounder:
				Player.buffImmune[BuffID.OnFire] = true;
				Player.GetDamage(DamageClass.Generic) += HasAspect(MobRelicAspect.Cinder) ? 0.10f : 0.06f;
				break;
			case SynergyArmorSetId.StormConductor:
				Player.moveSpeed += HasAspect(MobRelicAspect.Storm) ? 0.14f : 0.10f;
				Player.GetAttackSpeed(DamageClass.Generic) += 0.05f;
				break;
			case SynergyArmorSetId.MimicBroker:
				Player.GetCritChance(DamageClass.Generic) += 8f;
				Player.luck += tierCounts[(int)MobRelicTier.CommonCurio] >= 2 ? 0.18f : 0.10f;
				break;
			case SynergyArmorSetId.FrostRelay:
				Player.statDefense += 8;
				Player.endurance += HasAspect(MobRelicAspect.Frost) ? 0.06f : 0.04f;
				break;
			case SynergyArmorSetId.RootclockGardener:
				Player.statLifeMax2 += 25;
				Player.lifeRegen += HasAspect(MobRelicAspect.Verdant) || HasAspect(MobRelicAspect.Tidal) ? 6 : 3;
				break;
			case SynergyArmorSetId.TempleParadox:
				Player.statDefense += 10;
				Player.endurance += HasAspect(MobRelicAspect.Radiant) && HasAspect(MobRelicAspect.Umbral) ? 0.08f : 0.04f;
				break;
			case SynergyArmorSetId.EventHorizonMason:
				Player.GetDamage(DamageClass.Generic) += HasAspect(MobRelicAspect.Umbral) ? 0.16f : 0.10f;
				break;
			case SynergyArmorSetId.LunarCartel:
				Player.GetDamage(DamageClass.Generic) += 0.10f + (commissionTicks > 0 ? 0.15f : 0f);
				Player.maxMinions += 1;
				break;
			case SynergyArmorSetId.MoonlordThreshold:
				Player.GetDamage(DamageClass.Generic) += relicConvergenceEnabled ? 0.18f : 0.12f;
				Player.statLifeMax2 += 40;
				Player.statManaMax2 += 40;
				break;
			case SynergyArmorSetId.LegendaryBellArchitect:
				ApplyArchitectPhase();
				break;
		}
	}
}
