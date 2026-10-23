using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic spatial scoring and fail-closed distribution acceptance.
/// Collision clearance stays at 12 tiles; visual spacing is a separate, larger
/// constraint derived from authored focus size and scale class.
/// </summary>
public static partial class AuthoredResidenceLayoutPolicy
{
	public const int MacroSectorCount = 12;
	public const int WorldMargin = 40;
	public const int MaxSectorMass = 24;
	public const int MaxIndependentPerSectorLayer = 4;
	public const int MaxMegaPerSectorLayer = 1;
	public const int MaxAdjacentLandmarkMega = 3;
	// Two Hallow megas must not overlap or read as one landmark, but a 1200-tile
	// exclusion made otherwise safe FTW layouts seed-dependent. One macro-sector
	// still leaves a deliberately large visual interval while keeping both shelves usable.
	public const int HallowMegaShelfSeparation = 700;
	// Owner v10p stacked Archmage/Deviantt at |dx|~15. Hard min-dx=160 and
	// same-sector mega pairs both fail-closed Surface (0/61). Keep this as
	// a scoring preference only so technical admission can still PASS.
	public const int MinimumSurfaceMegaCenterSeparation = 160;
	// Keep the Large allowance, scale vertical terrain range beyond Large.
	// Mainland support, not this global band, distinguishes hills from islands.
	public static int MaximumSurfaceElevationAboveWorldSurface =>
		Worldgen.NativeSurfaceFloorSearch.ElevationAllowance(Main.maxTilesY);
	public const int MaximumCoastalWaterlineElevationAboveWorldSurface = 480;
	public const int MaximumSurfaceDepthBelowWorldSurface = 96;
	// Grounded buildings are bounded by their actual protected footprint and
	// WorldMargin, not the top of a source canvas containing optional empty sky.
	// The separate sky-stratum/ceiling population constraints remain unchanged.
	// Owner v10: HighSky may occupy the true high band, but three islands lined
	// up at y<110 are a ceiling shelf, not altitude variety.
	public const int HighSkyCeilingY = 110;
	public const int MaxHighSkySitesAtCeiling = 2;
	public const int MinimumIsolatedProtectedGap = 1;
	// Seven authored sky residences must use all three sky strata, while still
	// leaving the full-pack planner enough biome/spacing freedom to find a
	// solution. A cap of three made the real 67-mod domain unsatisfiable; four
	// still rejects the old 5/1/1 pile-up and admits balanced 4/2/1 layouts.
	public const int MaxSkySitesPerAltitudeBand = 4;
	public const int MinimumSkySites = 6;
	public const int MinimumCavernSites = 6;
	public const int MinimumUnderworldSites = 2;
}
