using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static class DamageProfileRegistry
{
	public static DamageSourceProfile KineticContact { get; } = DamageSourceProfile.Single(
		"dingdong:source/kinetic_contact",
		DamageTags.KineticContact);

	public static DamageSourceProfile Arrow { get; } = DamageSourceProfile.Single(
		"dingdong:source/arrow",
		DamageTags.Arrow);

	public static DamageSourceProfile Bullet { get; } = DamageSourceProfile.Single(
		"dingdong:source/bullet",
		DamageTags.Bullet);

	public static DamageSourceProfile SlimeStaffCreatureDrop { get; } = new(
		"dingdong:source/slime_staff_creature_drop",
		WeightedDamageAxis.Single("dingdong:axis/delivery", DamageTags.MinionContact),
		WeightedDamageAxis.Single("dingdong:axis/element", DamageTags.Gel),
		WeightedDamageAxis.Single("dingdong:axis/behavior", DamageTags.Sustained),
		WeightedDamageAxis.Single("dingdong:axis/acquisition", DamageTags.CreatureDrop));

	public static DamageSourceProfile FinchStaffWorldgen { get; } = new(
		"dingdong:source/finch_staff_worldgen",
		WeightedDamageAxis.Single("dingdong:axis/delivery", DamageTags.MinionContact),
		WeightedDamageAxis.Single("dingdong:axis/element", DamageTags.Nature),
		WeightedDamageAxis.Single("dingdong:axis/behavior", DamageTags.Sustained),
		WeightedDamageAxis.Single("dingdong:axis/acquisition", DamageTags.Worldgen));

	public static DamageTargetProfile VanillaSlime { get; } = new(
		"dingdong:target/vanilla_slime",
		new[] { DamageTags.Gelatinous },
		deliveryMultipliers: new Dictionary<string, double>
		{
			[DamageTags.KineticContact] = 0.85d,
			[DamageTags.Arrow] = 0.90d,
			[DamageTags.Bullet] = 0.95d
		},
		elementMultipliers: new Dictionary<string, double>
		{
			[DamageTags.Frost] = 1.10d,
			[DamageTags.Gel] = 0.85d
		},
		minimumMultiplier: 0.80d,
		maximumMultiplier: 1.30d);
}
