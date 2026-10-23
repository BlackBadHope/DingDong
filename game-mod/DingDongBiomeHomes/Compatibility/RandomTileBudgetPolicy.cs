namespace DingDongBiomeHomes.Compatibility;

// A budget operand only: never substitute dimensions or truncate the sampling domain.
internal static class RandomTileBudgetPolicy
{
	internal const int VanillaLargeArea = 8400 * 2400;
	internal static int NormalizeArea(int nativeArea, bool enabled) =>
		enabled && nativeArea > VanillaLargeArea ? VanillaLargeArea : nativeArea;
}
