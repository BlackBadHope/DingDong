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
	private static DamageSourceProfile CreateSource(string key, string delivery, string element, string behavior, string acquisition) => new(key, WeightedDamageAxis.Single("dingdong:axis/delivery", delivery), WeightedDamageAxis.Single("dingdong:axis/element", element), WeightedDamageAxis.Single("dingdong:axis/behavior", behavior), WeightedDamageAxis.Single("dingdong:axis/acquisition", acquisition));
	private static DamageTargetProfile CreateTarget(string key, string morphology, Dictionary<string, double> delivery, Dictionary<string, double> element) => new($"dingdong:target/{key}", new[] { morphology }, deliveryMultipliers: delivery, elementMultipliers: element, minimumMultiplier: 0.80d, maximumMultiplier: 1.30d);
}
