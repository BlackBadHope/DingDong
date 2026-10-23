using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Fishing;

// Pure decisions shared by runtime and offline fixtures. Water depth never implies map layer.
public static class FishingPolicy
{
	public static FishingMapLayer MapLayer(int y, double surface, double rock, int underworld) =>
		y >= underworld ? FishingMapLayer.Underworld : y >= rock ? FishingMapLayer.Cavern :
		y >= surface ? FishingMapLayer.Underground : FishingMapLayer.Surface;

	public static int TargetDepth(int requested, int column) => Math.Clamp(requested, 0, Math.Max(0, column - 2));
	public static FishingHabitat OceanHabitat(int edgeDistance, int width, int depth, int columnHeight) =>
		edgeDistance * 10 >= width * 7 || columnHeight < 32 ? FishingHabitat.CoastalShoals :
		edgeDistance * 20 <= width * 7 || depth >= 160 ? FishingHabitat.DeepOcean : FishingHabitat.OpenOcean;
	public static float ApproachWaterline(float currentY, float targetY) => currentY + Math.Clamp(targetY - currentY, -3f, 3f);
	public static bool Eligible(in FishingRewardSpec r, in FishingConditions c) =>
		r.Habitat == c.Habitat && c.WaterDepthTiles >= r.MinimumWaterDepth &&
		(r.MapLayer == null || r.MapLayer == c.MapLayer) &&
		(r.MaximumWaterDepth <= 0 || c.WaterDepthTiles <= r.MaximumWaterDepth) &&
		c.Progression >= r.RequiredProgression && c.FishingPower >= r.MinimumPower &&
		(!r.NightOnly || c.Night) && (!r.RainOnly || c.Weather != FishingWeather.Clear) &&
		(string.IsNullOrEmpty(r.EventId) || string.Equals(r.EventId, c.EventId, StringComparison.Ordinal));

	public static int Weight(in FishingRewardSpec r, in FishingConditions c)
	{
		if (!Eligible(r, c)) return 0;
		int weight = Math.Clamp(r.Weight, 1, 10000);
		if (!string.IsNullOrEmpty(r.EventId)) weight *= 3;
		return weight;
	}

	public static FishingRewardSpec? Pick(IReadOnlyList<FishingRewardSpec> rewards, FishingConditions c, int roll)
	{
		int total = 0;
		foreach (var r in rewards) total = checked(total + Weight(r, c));
		if (total == 0) return null;
		roll = (int)((uint)roll % (uint)total);
		foreach (var r in rewards)
		{
			roll -= Weight(r, c);
			if (roll < 0) return r;
		}
		return null;
	}
}
