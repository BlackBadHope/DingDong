using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchAbandonedVillageScan(ILContext il)
	{
		ILCursor origin = new(il);
		Expect(origin.TryGotoNext(MoveType.After,
			i => i.MatchCall("Terraria.WorldGen", "GetWorldSize"),
			i => i.MatchLdcI4(2), i => i.OpCode == OpCodes.Add,
			i => i.MatchLdcI4(75), i => i.OpCode == OpCodes.Mul,
			i => i.MatchStloc(5)), "SOTS evil scan origin");
		origin.Emit(OpCodes.Ldloc, il.Body.Variables[5]);
		origin.EmitDelegate<Func<int, int>>(ScaleEvilScanTop);
		origin.Emit(OpCodes.Stloc, il.Body.Variables[5]);

		ILCursor clamps = new(il);
		int clampCount = 0;
		while (clamps.TryGotoNext(MoveType.After, i => i.MatchLdcI4(500)))
		{
			clamps.EmitDelegate<Func<int, int>>(ScaleEvilCandidateTopLimit);
			clampCount++;
		}
		Expect(clampCount == 2, $"SOTS evil scan top constants {clampCount}/2");

		ILCursor bottom = new(il);
		Expect(bottom.TryGotoNext(MoveType.After, i => i.MatchLdcI4(800)),
			"SOTS evil scan bottom");
		bottom.EmitDelegate<Func<int, int>>(ScaleEvilScanBottom);
	}

	private static int ScaleEvilScanTop(int original)
	{
		if (!ContinentalAdapterMath.Active()) return original;
		SotsWorldgenReceipt.MarkEvilScan();
		return Math.Clamp((int)GenVars.worldSurfaceLow - 160, 100,
			NativeUnderworldSystem.Layer - 300);
	}

	private static int ScaleEvilScanBottom(int original)
	{
		if (!ContinentalAdapterMath.Active()) return original;
		return Math.Clamp((int)GenVars.worldSurfaceHigh + 650, 600,
			NativeUnderworldSystem.Layer);
	}

	private static int ScaleEvilCandidateTopLimit(int original)
	{
		if (!ContinentalAdapterMath.Active()) return original;
		if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive) return ScaleEvilScanTop(original);
		// This is an UPPER limit on the found candidate's top, not the scan's
		// lower bound. Keep its native 200-tile window inside the searched band.
		return Math.Max(ScaleEvilScanTop(original), ScaleEvilScanBottom(800) - 200);
	}
}
