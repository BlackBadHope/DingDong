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
	public static int SectorOf(int x)
	{
		int playable = Math.Max(1, Main.maxTilesX - WorldMargin * 2);
		int width = Math.Max(1, playable / MacroSectorCount);
		int sector = (x - WorldMargin) / width;
		if (sector < 0)
			return 0;
		if (sector >= MacroSectorCount)
			return MacroSectorCount - 1;
		return sector;
	}

	public static int MaxIndependentPerSector(BroadLayoutLayer layer) => layer switch
	{
		// Seven sky residences are required to cover at least five macro sectors.
		// Therefore the accepted cadence is 2+2+1+1+1, not seven distinct
		// sectors. Hard visual spacing and mutation-overlap checks still decide
		// whether the two residences can actually share a wide macro sector.
		BroadLayoutLayer.Sky => 2,
		BroadLayoutLayer.Cavern => 2,
		BroadLayoutLayer.Underworld => 1,
		// Surface isolation is pairwise (asset gap + mega/landmark sector). A
		// numeric Isolated cap of 2 starved Forest/Jungle shelves on Large FTW.
		_ => int.MaxValue
	};
	public static int MaxSitesPerSector(BroadLayoutLayer layer) => layer switch
	{
		// Keep this consistent with RequiredDistinctSectors(Sky, 7) == 5.
		// The previous value of one made valid 2+2+1+1+1 layouts impossible and
		// turned narrow coastal/Hallow sky domains into a pigeonhole failure.
		BroadLayoutLayer.Sky => 2,
		BroadLayoutLayer.Cavern => 2,
		BroadLayoutLayer.Underworld => 1,
		// Surface capacity is mass-driven: a compact house and a mega-scene are
		// not equivalent units. Horizontal coverage and visual spacing remain hard.
		_ => int.MaxValue
	};
	public static int RequiredDistinctSectors(BroadLayoutLayer layer, int total) => layer switch
	{
		BroadLayoutLayer.Sky => Math.Min(total, 5),
		BroadLayoutLayer.Surface => Math.Min(total, 10),
		BroadLayoutLayer.Cavern => Math.Min(total, 5),
		BroadLayoutLayer.Underworld => Math.Min(total, 2),
		_ => 0
	};
}
