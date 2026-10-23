using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed record RapidIndependentHitRule
{
	public RapidIndependentHitRule(string id, int requiredHits, int windowTicks, double overrideMultiplier)
	{
		DamageTagValidation.RequireNamespaced(id, nameof(id));
		if (requiredHits < 2)
			throw new ArgumentOutOfRangeException(nameof(requiredHits), requiredHits, "A rapid-hit rule must require at least two hits.");
		if (windowTicks < 1)
			throw new ArgumentOutOfRangeException(nameof(windowTicks), windowTicks, "The hit window must contain at least one tick.");
		if (!double.IsFinite(overrideMultiplier) || overrideMultiplier <= 0d)
			throw new ArgumentOutOfRangeException(nameof(overrideMultiplier), overrideMultiplier, "The override multiplier must be finite and greater than zero.");
		Id = id;
		RequiredHits = requiredHits;
		WindowTicks = windowTicks;
		OverrideMultiplier = overrideMultiplier;
	}

	public string Id { get; }
	public int RequiredHits { get; }
	public int WindowTicks { get; }
	public double OverrideMultiplier { get; }
}
