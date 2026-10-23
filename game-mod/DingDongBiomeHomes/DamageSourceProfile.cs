using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed class DamageSourceProfile
{
	public DamageSourceProfile(string key, WeightedDamageAxis delivery, WeightedDamageAxis element, WeightedDamageAxis behavior, WeightedDamageAxis acquisition)
	{
		DamageTagValidation.RequireNamespaced(key, nameof(key));
		Key = key;
		Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
		Element = element ?? throw new ArgumentNullException(nameof(element));
		Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
		Acquisition = acquisition ?? throw new ArgumentNullException(nameof(acquisition));
	}

	public string Key { get; }
	public WeightedDamageAxis Delivery { get; }
	public WeightedDamageAxis Element { get; }
	public WeightedDamageAxis Behavior { get; }
	public WeightedDamageAxis Acquisition { get; }

	public static DamageSourceProfile Single(string key, string delivery, string element = DamageTags.Neutral, string behavior = DamageTags.Single, string acquisition = DamageTags.UnknownAcquisition) => new(key, WeightedDamageAxis.Single("dingdong:axis/delivery", delivery), WeightedDamageAxis.Single("dingdong:axis/element", element), WeightedDamageAxis.Single("dingdong:axis/behavior", behavior), WeightedDamageAxis.Single("dingdong:axis/acquisition", acquisition));
}
