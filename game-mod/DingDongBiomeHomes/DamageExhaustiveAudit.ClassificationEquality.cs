using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Dedicated-only exhaustive compatibility gate. Every loaded damage-capable
/// item, projectile and debuff source is evaluated against every loaded
/// hostile NPC target through the production rule engine. The audit never
/// mutates world, player, item or NPC persistence state.
/// </summary>
internal static partial class DamageExhaustiveAudit
{
	private static bool SameSourceClassification(DamageSourceClassification left, DamageSourceClassification right) => left.Coverage == right.Coverage && left.TrueMultistrike == right.TrueMultistrike && SameSourceProfile(left.Profile, right.Profile);
	private static bool SameTargetClassification(DamageTargetClassification left, DamageTargetClassification right) => left.Coverage == right.Coverage && left.SegmentRole == right.SegmentRole && left.PhaseRole == right.PhaseRole && left.EncounterRootId == right.EncounterRootId && SameTargetProfile(left.Profile, right.Profile);
	private static bool SameSourceProfile(DamageSourceProfile left, DamageSourceProfile right) => string.Equals(left.Key, right.Key, StringComparison.Ordinal) && SameAxis(left.Delivery, right.Delivery) && SameAxis(left.Element, right.Element) && SameAxis(left.Behavior, right.Behavior) && SameAxis(left.Acquisition, right.Acquisition);
	private static bool SameTargetProfile(DamageTargetProfile left, DamageTargetProfile right) => string.Equals(left.Key, right.Key, StringComparison.Ordinal) && left.Morphology.SequenceEqual(right.Morphology, StringComparer.Ordinal) && SameDictionary(left.DeliveryMultipliers, right.DeliveryMultipliers) && SameDictionary(left.ElementMultipliers, right.ElementMultipliers) && SameDictionary(left.BehaviorMultipliers, right.BehaviorMultipliers) && SameDictionary(left.AcquisitionMultipliers, right.AcquisitionMultipliers) && SameDictionary(left.SegmentMultipliers, right.SegmentMultipliers) && SameDictionary(left.PhaseMultipliers, right.PhaseMultipliers) && SameRapidRule(left.RapidHitRule, right.RapidHitRule) && Nearly(left.MinimumMultiplier, right.MinimumMultiplier) && Nearly(left.MaximumMultiplier, right.MaximumMultiplier);
	private static bool SameAxis(WeightedDamageAxis left, WeightedDamageAxis right) => string.Equals(left.AxisName, right.AxisName, StringComparison.Ordinal) && left.Tags.Count == right.Tags.Count && left.Tags.Zip(right.Tags).All(pair => string.Equals(pair.First.Id, pair.Second.Id, StringComparison.Ordinal) && Nearly(pair.First.Weight, pair.Second.Weight));
	private static bool SameDictionary<TKey>(IReadOnlyDictionary<TKey, double> left, IReadOnlyDictionary<TKey, double> right)
		where TKey : notnull => left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out double value) && Nearly(pair.Value, value));
	private static bool SameRapidRule(RapidIndependentHitRule left, RapidIndependentHitRule right)
	{
		if (left == null || right == null)
			return left == right;
		return string.Equals(left.Id, right.Id, StringComparison.Ordinal) && left.RequiredHits == right.RequiredHits && left.WindowTicks == right.WindowTicks && Nearly(left.OverrideMultiplier, right.OverrideMultiplier);
	}
}
