using System;
using MonoMod.Cil;
using DingDongBiomeHomes.OceanExpeditions;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	// V03 29.10: SOTS 0.25.1.9 DeterminePyramidLocation (runs in "SOTS: Structures").
	private const string PyramidSiteHash = "7039754FEC3FD26D208E37E576791C362435AB27A70794F15DAAAA9640CC866F";

	// V03 29.10: every x roll (2 initial + 2 retry) passes the open-ocean basin guard.
	private static void PatchPyramidOceanSite(ILContext il)
	{
		ILCursor cursor = new(il);
		int rolls = 0;
		while (cursor.TryGotoNext(MoveType.After, i => i.MatchCallvirt("Terraria.Utilities.UnifiedRandom", "Next")))
		{
			cursor.EmitDelegate<Func<int, int>>(OceanForeignFootprint.AdmitPyramidX);
			rolls++;
		}
		if (rolls != 4)
			throw new InvalidOperationException($"SOTS pyramid site roll IL pattern changed: {rolls}/4.");
	}
}
