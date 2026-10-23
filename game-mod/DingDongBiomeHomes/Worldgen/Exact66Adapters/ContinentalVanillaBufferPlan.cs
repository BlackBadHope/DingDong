namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly record struct ContinentalVanillaBufferPlan(
	int LarvaCapacity,
	int JungleChestCapacity,
	int SkyFeatureCapacity,
	int MountainCaveCapacity,
	int EvilHeartCapacity)
{
	internal static ContinentalVanillaBufferPlan Create(int width, int height)
	{
		long tiles = checked((long)width * height);
		long area = ContinentalScaleProfile.AreaScale(tiles);
		long horizontal = ContinentalScaleProfile.WidthScale(width);
		return new ContinentalVanillaBufferPlan(
			ContinentalScaleProfile.Capacity(100, area),
			ContinentalScaleProfile.Capacity(100, area),
			ContinentalScaleProfile.Capacity(30, horizontal),
			ContinentalScaleProfile.Capacity(30, horizontal),
			ContinentalScaleProfile.Capacity(100, horizontal));
	}
}
