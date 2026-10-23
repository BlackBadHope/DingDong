using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DingDongBiomeHomes;
public sealed partial class DamageTargetProfile
{
	private readonly ReadOnlyCollection<string> morphology;
	private readonly ReadOnlyDictionary<string, double> deliveryMultipliers;
	private readonly ReadOnlyDictionary<string, double> elementMultipliers;
	private readonly ReadOnlyDictionary<string, double> behaviorMultipliers;
	private readonly ReadOnlyDictionary<string, double> acquisitionMultipliers;
	private readonly ReadOnlyDictionary<DamageSegmentRole, double> segmentMultipliers;
	private readonly ReadOnlyDictionary<DamagePhaseRole, double> phaseMultipliers;
	public DamageTargetProfile(string key, IEnumerable<string> morphology, IEnumerable<KeyValuePair<string, double>> deliveryMultipliers = null, IEnumerable<KeyValuePair<string, double>> elementMultipliers = null, IEnumerable<KeyValuePair<string, double>> behaviorMultipliers = null, IEnumerable<KeyValuePair<string, double>> acquisitionMultipliers = null, IEnumerable<KeyValuePair<DamageSegmentRole, double>> segmentMultipliers = null, IEnumerable<KeyValuePair<DamagePhaseRole, double>> phaseMultipliers = null, RapidIndependentHitRule rapidHitRule = null, double minimumMultiplier = 0.80d, double maximumMultiplier = 1.30d)
	{
		DamageTagValidation.RequireNamespaced(key, nameof(key));
		ArgumentNullException.ThrowIfNull(morphology);
		if (!double.IsFinite(minimumMultiplier) || !double.IsFinite(maximumMultiplier) || minimumMultiplier <= 0d || maximumMultiplier < minimumMultiplier)
		{
			throw new ArgumentOutOfRangeException(nameof(minimumMultiplier), "Damage multiplier clamp is invalid.");
		}

		string[] morphologyArray = morphology.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
		foreach (string tag in morphologyArray)
			DamageTagValidation.RequireNamespaced(tag, nameof(morphology));
		Key = key;
		this.morphology = Array.AsReadOnly(morphologyArray);
		this.deliveryMultipliers = CopyMultipliers(deliveryMultipliers, nameof(deliveryMultipliers));
		this.elementMultipliers = CopyMultipliers(elementMultipliers, nameof(elementMultipliers));
		this.behaviorMultipliers = CopyMultipliers(behaviorMultipliers, nameof(behaviorMultipliers));
		this.acquisitionMultipliers = CopyMultipliers(acquisitionMultipliers, nameof(acquisitionMultipliers));
		this.segmentMultipliers = CopySegmentMultipliers(segmentMultipliers);
		this.phaseMultipliers = CopyPhaseMultipliers(phaseMultipliers);
		RapidHitRule = rapidHitRule;
		MinimumMultiplier = minimumMultiplier;
		MaximumMultiplier = maximumMultiplier;
	}

	public string Key { get; }
	public IReadOnlyList<string> Morphology => morphology;
	public IReadOnlyDictionary<string, double> DeliveryMultipliers => deliveryMultipliers;
	public IReadOnlyDictionary<string, double> ElementMultipliers => elementMultipliers;
	public IReadOnlyDictionary<string, double> BehaviorMultipliers => behaviorMultipliers;
	public IReadOnlyDictionary<string, double> AcquisitionMultipliers => acquisitionMultipliers;
	public IReadOnlyDictionary<DamageSegmentRole, double> SegmentMultipliers => segmentMultipliers;
	public IReadOnlyDictionary<DamagePhaseRole, double> PhaseMultipliers => phaseMultipliers;
	public RapidIndependentHitRule RapidHitRule { get; }
	public double MinimumMultiplier { get; }
	public double MaximumMultiplier { get; }
}
