using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed class DamageRuleEngine
{
	public DamageEvaluationTrace EvaluateAcceptedHit(DamageSourceProfile source, DamageTargetProfile target, DamageHitObservation hit, DamageHitLedger ledger, DamageSegmentRole segmentRole = DamageSegmentRole.None, DamagePhaseRole phaseRole = DamagePhaseRole.None)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(target);
		ArgumentNullException.ThrowIfNull(ledger);
		double delivery = WeightedGeometricMean(source.Delivery, target.DeliveryMultipliers);
		double element = WeightedGeometricMean(source.Element, target.ElementMultipliers);
		double behavior = WeightedGeometricMean(source.Behavior, target.BehaviorMultipliers);
		double acquisition = WeightedGeometricMean(source.Acquisition, target.AcquisitionMultipliers);
		double segment = target.SegmentMultipliers.TryGetValue(segmentRole, out double segmentValue) ? segmentValue : 1d;
		double phase = target.PhaseMultipliers.TryGetValue(phaseRole, out double phaseValue) ? phaseValue : 1d;
		HitWindowResult window = default;
		string condition = "none";
		double raw = delivery * element * behavior * acquisition * segment * phase;
		bool participatesInRapidHitWindow = target.RapidHitRule != null && !ContainsTag(source.Delivery, DamageTags.DamageOverTime);
		if (participatesInRapidHitWindow)
		{
			window = ledger.Register(hit, target.RapidHitRule.RequiredHits, target.RapidHitRule.WindowTicks);
			if (window.Triggered)
			{
				condition = target.RapidHitRule.Id;
				raw = target.RapidHitRule.OverrideMultiplier;
			}
		}

		double final = Math.Clamp(raw, target.MinimumMultiplier, target.MaximumMultiplier);
		return new DamageEvaluationTrace(source.Key, target.Key, delivery, element, behavior, acquisition, segment, phase, condition, raw, final, Math.Abs(raw - final) > 0.0000001d, !participatesInRapidHitWindow || window.AcceptedIndependentEvent, window.IndependentHitsInSequence);
	}

	private static double WeightedGeometricMean(WeightedDamageAxis axis, IReadOnlyDictionary<string, double> multipliers)
	{
		double weightedLog = 0d;
		foreach (WeightedDamageTag tag in axis.Tags)
		{
			double multiplier = multipliers.TryGetValue(tag.Id, out double configured) ? configured : 1d;
			weightedLog += tag.Weight * Math.Log(multiplier);
		}

		return Math.Exp(weightedLog);
	}

	private static bool ContainsTag(WeightedDamageAxis axis, string tagId) => axis.Tags.Any(tag => string.Equals(tag.Id, tagId, StringComparison.Ordinal));
}
