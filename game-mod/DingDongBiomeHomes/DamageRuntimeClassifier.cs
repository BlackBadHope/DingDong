using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Data-driven runtime classifier. Optional-mod content is inspected only via
/// public Terraria/tModLoader metadata, so no compatibility assembly is a hard
/// dependency.
/// </summary>
public static partial class DamageRuntimeClassifier
{
	private static readonly DamageTargetProfile OrganicTarget = CreateTarget("organic", "dingdong:morphology/organic", delivery: new() { [DamageTags.Arrow] = 1.03d, [DamageTags.Bullet] = 0.97d }, element: new() { [DamageTags.Fire] = 1.05d, [DamageTags.Frost] = 1.05d, [DamageTags.Poison] = 1.10d });
	private static readonly DamageTargetProfile ArmoredTarget = CreateTarget("armored", "dingdong:morphology/armored", delivery: new() { [DamageTags.KineticContact] = 0.90d, [DamageTags.Arrow] = 0.85d, [DamageTags.Bullet] = 0.95d, [DamageTags.Explosion] = 1.15d }, element: new() { [DamageTags.Lightning] = 1.12d });
	private static readonly DamageTargetProfile UndeadTarget = CreateTarget("undead", "dingdong:morphology/undead", delivery: new() { [DamageTags.KineticContact] = 0.95d, [DamageTags.Arrow] = 0.97d, [DamageTags.Bullet] = 0.92d }, element: new() { [DamageTags.Holy] = 1.25d, [DamageTags.Fire] = 1.15d, [DamageTags.Poison] = 0.80d, [DamageTags.Shadow] = 0.90d });
	private static readonly DamageTargetProfile PlantTarget = CreateTarget("plant", "dingdong:morphology/plant", delivery: new() { [DamageTags.KineticContact] = 0.90d, [DamageTags.Arrow] = 0.95d, [DamageTags.Bullet] = 0.88d }, element: new() { [DamageTags.Fire] = 1.30d, [DamageTags.Frost] = 1.10d, [DamageTags.Poison] = 0.85d, [DamageTags.Nature] = 0.85d });
	private static readonly DamageTargetProfile AquaticTarget = CreateTarget("aquatic", "dingdong:morphology/aquatic", delivery: new() { [DamageTags.Bullet] = 0.95d }, element: new() { [DamageTags.Fire] = 0.82d, [DamageTags.Lightning] = 1.25d, [DamageTags.Frost] = 1.12d, [DamageTags.Water] = 0.85d });
	private static readonly DamageTargetProfile SpectralTarget = CreateTarget("spectral", "dingdong:morphology/spectral", delivery: new() { [DamageTags.KineticContact] = 0.80d, [DamageTags.Arrow] = 0.85d, [DamageTags.Bullet] = 0.82d, [DamageTags.MagicProjectile] = 1.20d }, element: new() { [DamageTags.Holy] = 1.30d, [DamageTags.Shadow] = 0.80d });
	private static readonly DamageTargetProfile MechanicalTarget = CreateTarget("mechanical", "dingdong:morphology/mechanical", delivery: new() { [DamageTags.KineticContact] = 0.90d, [DamageTags.Arrow] = 0.85d, [DamageTags.Bullet] = 0.95d, [DamageTags.Explosion] = 1.18d }, element: new() { [DamageTags.Lightning] = 1.18d, [DamageTags.Poison] = 0.80d });
}
