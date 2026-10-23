using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;

public sealed partial class SynergyArmorPlayer
{
	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) => HandleHit(target, damageDone, hit.Crit, item.DamageType);

	public override void OnHitNPCWithProj(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (projectile.ModProjectile is SynergyArmorPulseProjectile)
			return;
		HandleHit(target, damageDone, hit.Crit, projectile.DamageType);
	}

	internal int ForceMechanicProbe(SynergyArmorSetId setId, NPC target, out string signature)
	{
		activeSet = SynergyArmorCatalog.RegisteredSets[setId].Definition;
		int damage = 30 + (int)setId * 3;
		int index = SpawnPulse(target, damage, (SynergyArmorPulseMode)setId, delay: setId is SynergyArmorSetId.FrostRelay or SynergyArmorSetId.MoonlordThreshold ? 24 : 0);
		signature = $"{setId}:{(SynergyArmorPulseMode)setId}:{damage}:{index}";
		return index;
	}

	private void HandleHit(NPC target, int damageDone, bool critical, DamageClass damageClass)
	{
		if (activeSet == null || target == null || !target.active || target.friendly || damageDone <= 0)
			return;
		switch (activeSet.Id)
		{
			case SynergyArmorSetId.ThresholdForager: ReleaseTravelBriar(target, damageDone); break;
			case SynergyArmorSetId.PrismGlasswright: ReleasePrismVolley(target, damageDone, critical); break;
			case SynergyArmorSetId.BurrowBell: ReleaseBurrowBell(target, damageDone); break;
			case SynergyArmorSetId.HiveCantor: ConductHiveVolley(target, damageDone); break;
			case SynergyArmorSetId.DungeonCartographer: DrawDamageRoute(target, damageDone, damageClass); break;
			case SynergyArmorSetId.StormConductor: ReleaseStormChain(target, damageDone); break;
			case SynergyArmorSetId.MimicBroker: SettleMimicCredit(target, damageDone, critical); break;
			case SynergyArmorSetId.FrostRelay: RelayFrostHit(target, damageDone); break;
			case SynergyArmorSetId.RootclockGardener: StoreRootclockDamage(target, damageDone); break;
			case SynergyArmorSetId.TempleParadox: AccelerateParadox(); break;
			case SynergyArmorSetId.EventHorizonMason: AccumulateHorizonMass(target, damageDone); break;
			case SynergyArmorSetId.LunarCartel: AdvanceLunarCommission(target, damageDone, damageClass); break;
			case SynergyArmorSetId.MoonlordThreshold: ReleaseThresholdEchoes(target, damageDone); break;
			case SynergyArmorSetId.LegendaryBellArchitect: StrikeArchitectBell(target, damageDone); break;
		}
	}
}
