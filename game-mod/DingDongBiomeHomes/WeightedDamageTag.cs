using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public readonly record struct WeightedDamageTag
{
	public WeightedDamageTag(string id, double weight)
	{
		DamageTagValidation.RequireNamespaced(id, nameof(id));
		if (!double.IsFinite(weight) || weight <= 0d)
			throw new ArgumentOutOfRangeException(nameof(weight), weight, "Damage tag weight must be finite and greater than zero.");
		Id = id;
		Weight = weight;
	}

	public string Id { get; }
	public double Weight { get; }
}
