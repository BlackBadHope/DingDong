namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SotsVillageGroundPolicy
{
	// Stable Terraria tile IDs: dirt, stone, grass, sand, mud, jungle grass,
	// mushroom grass, snow, ice. Excludes ores, bricks, furniture and unknown tiles.
	internal static bool IsNatural(int type) => type is 0 or 1 or 2 or 53 or 59 or 60 or 70 or 147 or 161;
}
