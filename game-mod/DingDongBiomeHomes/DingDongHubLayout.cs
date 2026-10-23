namespace DingDongBiomeHomes;

// Logical UI pixels, not screen pixels: the parent already accounts for UI Scale.
internal readonly record struct DingDongHubLayout(int Columns, float NavigationTop, float NavigationHeight, float ContentTop, bool Compact)
{
	internal const float TabHeight = 44f;
	internal static DingDongHubLayout Calculate(float innerWidth, float innerHeight)
	{
		int columns = innerWidth < 400f ? 2 : 4;
		bool compact = innerHeight < 440f;
		float top = compact ? 42f : 62f;
		float height = columns == 2 ? TabHeight * 2f + 6f : TabHeight;
		return new(columns, top, height, top + height + 14f, compact);
	}
}
