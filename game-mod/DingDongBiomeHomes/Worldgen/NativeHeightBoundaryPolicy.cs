namespace DingDongBiomeHomes.Worldgen;

// A height marker is not a feature size or an edge safety margin. Translate
// its distance from the legacy 200-tile biome ceiling, retaining local relief.
internal static class NativeHeightBoundaryPolicy
{
	internal static int Translate(int nativeOffset, int height, int geometryVersion) =>
		checked(NativeUnderworldPolicy.SavedOffset(200, height, geometryVersion) + nativeOffset - 200);
}
