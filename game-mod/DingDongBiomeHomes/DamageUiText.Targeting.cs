using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;

namespace DingDongBiomeHomes;

internal static partial class DamageUiText
{
	public static string TargetArchetype(DamageTargetProfile profile)
	{
		string key = profile.Key switch
		{
			"dingdong:target/vanilla_slime" => "Slime",
			"dingdong:target/organic" => "Organic",
			"dingdong:target/armored" => "Armored",
			"dingdong:target/undead" => "Undead",
			"dingdong:target/plant" => "Plant",
			"dingdong:target/aquatic" => "Aquatic",
			"dingdong:target/spectral" => "Spectral",
			"dingdong:target/mechanical" => "Mechanical",
			_ => "Unknown"
		};
		return Language.GetTextValue($"Mods.DingDongBiomeHomes.Damage.Guide.Archetypes.{key}");
	}

	public static List<(string Tag, double Multiplier)> Interactions(DamageTargetProfile profile, bool weaknesses)
	{
		return profile.DeliveryMultipliers.Concat(profile.ElementMultipliers)
			.Where(pair => weaknesses ? pair.Value >= 1.075d : pair.Value <= 0.925d)
			.OrderByDescending(pair => Math.Abs(Math.Log(pair.Value)))
			.ThenBy(pair => pair.Key, StringComparer.Ordinal)
			.Select(pair => (pair.Key, pair.Value))
			.ToList();
	}

	public static (string Tag, double Multiplier) DominantInteraction(DamageSourceProfile source, DamageTargetProfile target)
	{
		(string tag, double multiplier, double strength) best = (DamageTags.Neutral, 1d, 0d);
		ConsiderAxis(source.Delivery, target.DeliveryMultipliers, ref best);
		ConsiderAxis(source.Element, target.ElementMultipliers, ref best);
		return (best.tag, best.multiplier);
	}

	private static void ConsiderAxis(WeightedDamageAxis axis, IReadOnlyDictionary<string, double> multipliers, ref (string tag, double multiplier, double strength) best)
	{
		foreach (WeightedDamageTag tag in axis.Tags)
		{
			double multiplier = multipliers.TryGetValue(tag.Id, out double value) ? value : 1d;
			double strength = Math.Abs(Math.Log(multiplier)) * tag.Weight;
			if (strength > best.strength)
				best = (tag.Id, multiplier, strength);
		}
	}
}
