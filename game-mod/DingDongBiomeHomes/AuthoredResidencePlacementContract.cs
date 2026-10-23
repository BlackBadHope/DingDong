using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public sealed record AuthoredResidencePlacementContract(string PresetId, string NpcKey, ResidenceKind Kind, ResidenceScaleClass ScaleClass, IReadOnlyList<HomeBiome> AllowedBiomes, HomeBiome PreferredBiome, IReadOnlyList<ResidenceAltitudeBand> AllowedLayers, ResidenceAltitudeBand PreferredLayer, ResidenceAnchorMode AnchorMode, string NaturalContextRequirements, ResidenceClusterPolicy ClusterPolicy, int LandmarkWeight, int MinimumVisualDistance, ResidenceAltitudeBand PreferredAltitudeBand, int MaximumLocalDensity, bool CanShareSettlement, bool RequiresIsolation, int CadenceWeight, ResidenceMacroWindow MacroWindow)
{
	public int Mass => ScaleClass switch
	{
		ResidenceScaleClass.Compact => 1,
		ResidenceScaleClass.Medium => 2,
		ResidenceScaleClass.Large => 3,
		ResidenceScaleClass.Landmark => 5,
		ResidenceScaleClass.Mega => 8,
		_ => 1
	};
	public BroadLayoutLayer BroadLayer => PreferredLayer switch
	{
		ResidenceAltitudeBand.HighSky or ResidenceAltitudeBand.MidSky or ResidenceAltitudeBand.LowSky => BroadLayoutLayer.Sky,
		ResidenceAltitudeBand.Underworld => BroadLayoutLayer.Underworld,
		ResidenceAltitudeBand.Underground or ResidenceAltitudeBand.Cavern or ResidenceAltitudeBand.DeepCavern => BroadLayoutLayer.Cavern,
		_ => BroadLayoutLayer.Surface
	};
}
