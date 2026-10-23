using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed class WeightedDamageAxis
{
	private readonly ReadOnlyCollection<WeightedDamageTag> tags;
	public WeightedDamageAxis(string axisName, IEnumerable<WeightedDamageTag> sourceTags)
	{
		DamageTagValidation.RequireNamespaced(axisName, nameof(axisName));
		ArgumentNullException.ThrowIfNull(sourceTags);
		SortedDictionary<string, double> merged = new(StringComparer.Ordinal);
		foreach (WeightedDamageTag tag in sourceTags)
		{
			if (merged.TryGetValue(tag.Id, out double previous))
				merged[tag.Id] = previous + tag.Weight;
			else
				merged.Add(tag.Id, tag.Weight);
		}

		if (merged.Count == 0)
			throw new ArgumentException("A damage axis must contain at least one tag.", nameof(sourceTags));
		double total = merged.Values.Sum();
		tags = Array.AsReadOnly(merged.Select(pair => new WeightedDamageTag(pair.Key, pair.Value / total)).ToArray());
		AxisName = axisName;
	}

	public string AxisName { get; }
	public IReadOnlyList<WeightedDamageTag> Tags => tags;

	public static WeightedDamageAxis Single(string axisName, string tagId) => new(axisName, new[] { new WeightedDamageTag(tagId, 1d) });
}
