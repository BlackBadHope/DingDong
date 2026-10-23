using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DingDongBiomeHomes;
public readonly record struct DamageEvaluationTrace(string SourceKey, string TargetKey, double DeliveryMultiplier, double ElementMultiplier, double BehaviorMultiplier, double AcquisitionMultiplier, double SegmentMultiplier, double PhaseMultiplier, string ConditionalRule, double RawMultiplier, double FinalMultiplier, bool WasClamped, bool AcceptedIndependentEvent, int IndependentHitsInSequence)
{
	public string ToStableString()
	{
		CultureInfo invariant = CultureInfo.InvariantCulture;
		return string.Join('|', new[] { $"source={SourceKey}", $"target={TargetKey}", $"delivery={DeliveryMultiplier.ToString("0.000000", invariant)}", $"element={ElementMultiplier.ToString("0.000000", invariant)}", $"behavior={BehaviorMultiplier.ToString("0.000000", invariant)}", $"acquisition={AcquisitionMultiplier.ToString("0.000000", invariant)}", $"segment={SegmentMultiplier.ToString("0.000000", invariant)}", $"phase={PhaseMultiplier.ToString("0.000000", invariant)}", $"condition={ConditionalRule}", $"raw={RawMultiplier.ToString("0.000000", invariant)}", $"final={FinalMultiplier.ToString("0.000000", invariant)}", $"clamped={WasClamped}", $"independent={AcceptedIndependentEvent}", $"sequenceCount={IndependentHitsInSequence}" });
	}
}
