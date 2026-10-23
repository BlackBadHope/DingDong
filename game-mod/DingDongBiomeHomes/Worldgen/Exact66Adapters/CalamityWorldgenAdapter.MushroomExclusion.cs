using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchMushroomExclusion(ILContext il)
	{
		ILCursor start = new(il);
		if (!start.TryGotoNext(MoveType.AfterLabel,
			i => i.MatchLdloc(7), i => i.MatchLdloc(9), i => i.OpCode == OpCodes.Sub, i => i.MatchStloc(11)))
			throw new InvalidOperationException("Mushroom exclusion scan start changed.");
		Instruction original = start.Next;
		ILCursor end = new(il) { Index = start.Index };
		if (!end.TryGotoNext(MoveType.Before,
			i => i.MatchLdloc(6), i => i.OpCode == OpCodes.Brtrue_S,
			i => i.MatchLdcI4(0), i => i.MatchStloc(13)))
			throw new InvalidOperationException("Mushroom exclusion scan end changed.");
		Instruction after = end.Next;
		// Only bypass the pure exclusion predicate, never placement or RNG draws.
		start.EmitDelegate<Func<bool>>(ContinentalAdapterMath.Active);
		start.Emit(OpCodes.Brfalse, original);
		start.Emit(OpCodes.Ldloc, il.Body.Variables[7]);
		start.Emit(OpCodes.Ldloc, il.Body.Variables[8]);
		start.Emit(OpCodes.Ldloc, il.Body.Variables[9]);
		start.EmitDelegate<Func<int, int, int, bool>>(ScanMushroomExclusion);
		start.Emit(OpCodes.Stloc, il.Body.Variables[6]);
		start.Emit(OpCodes.Br, after);
	}

	private static bool ScanMushroomExclusion(int x, int y, int radius) =>
		MushroomExclusionScan.Rejected(x, y, radius, Main.maxTilesX, Main.maxTilesY,
			static (i, j) => Main.tile[i, j].TileType,
			static (i, j) => GenVars.UndergroundDesertLocation.Contains(i, j));
}
