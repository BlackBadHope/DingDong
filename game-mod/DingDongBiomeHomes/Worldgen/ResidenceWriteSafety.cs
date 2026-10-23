using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

internal static class ResidenceWriteSafety
{
	// Solid bounding rectangle of every tile/wall write, never sparse holes.
	// Native Terraria framing can cascade through neighbouring natural/multi-tile
	// objects beyond the two explicitly framed rows/columns.  Twelve tiles is the
	// observed closed envelope: a worldgen run changed the twelfth column beyond
	// a scene asset while the old eight-tile admission still accepted the site.
	internal const int Halo = 12;
	internal static Rectangle Bounds(Rectangle protectedArea, int spacingPadding)
	{
		if (spacingPadding < 0 || protectedArea.Width <= 2L * spacingPadding || protectedArea.Height <= 2L * spacingPadding)
			throw new System.ArgumentOutOfRangeException(nameof(spacingPadding));
		protectedArea.Inflate(Halo - spacingPadding, Halo - spacingPadding);
		return protectedArea;
	}
}
