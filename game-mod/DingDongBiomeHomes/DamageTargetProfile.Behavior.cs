using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed partial class DamageTargetProfile
{
	private static ReadOnlyDictionary<string, double> CopyMultipliers(IEnumerable<KeyValuePair<string, double>> source, string parameterName)
	{
		SortedDictionary<string, double> copy = new(StringComparer.Ordinal);
		if (source != null)
		{
			foreach (KeyValuePair<string, double> pair in source)
			{
				DamageTagValidation.RequireNamespaced(pair.Key, parameterName);
				if (!double.IsFinite(pair.Value) || pair.Value <= 0d)
					throw new ArgumentOutOfRangeException(parameterName, pair.Value, "A target multiplier must be finite and greater than zero.");
				if (!copy.TryAdd(pair.Key, pair.Value))
					throw new ArgumentException($"Duplicate target multiplier tag: {pair.Key}.", parameterName);
			}
		}

		return new ReadOnlyDictionary<string, double>(copy);
	}

	private static ReadOnlyDictionary<DamageSegmentRole, double> CopySegmentMultipliers(IEnumerable<KeyValuePair<DamageSegmentRole, double>> source)
	{
		SortedDictionary<DamageSegmentRole, double> copy = new();
		if (source != null)
		{
			foreach (KeyValuePair<DamageSegmentRole, double> pair in source)
			{
				if (!double.IsFinite(pair.Value) || pair.Value <= 0d)
					throw new ArgumentOutOfRangeException(nameof(source), pair.Value, "A segment multiplier must be finite and greater than zero.");
				if (!copy.TryAdd(pair.Key, pair.Value))
					throw new ArgumentException($"Duplicate segment role: {pair.Key}.", nameof(source));
			}
		}

		return new ReadOnlyDictionary<DamageSegmentRole, double>(copy);
	}

	private static ReadOnlyDictionary<DamagePhaseRole, double> CopyPhaseMultipliers(IEnumerable<KeyValuePair<DamagePhaseRole, double>> source)
	{
		SortedDictionary<DamagePhaseRole, double> copy = new();
		if (source != null)
		{
			foreach (KeyValuePair<DamagePhaseRole, double> pair in source)
			{
				if (!double.IsFinite(pair.Value) || pair.Value <= 0d)
					throw new ArgumentOutOfRangeException(nameof(source), pair.Value, "A phase multiplier must be finite and greater than zero.");
				if (!copy.TryAdd(pair.Key, pair.Value))
					throw new ArgumentException($"Duplicate phase role: {pair.Key}.", nameof(source));
			}
		}

		return new ReadOnlyDictionary<DamagePhaseRole, double>(copy);
	}
}
