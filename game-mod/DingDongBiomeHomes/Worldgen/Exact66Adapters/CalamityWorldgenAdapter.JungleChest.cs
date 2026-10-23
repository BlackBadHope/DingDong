using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchJungleChestAttempts(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After, i => IsMainField(i, "maxTilesX"),
			i => i.MatchLdcI4(10), i => i.OpCode == OpCodes.Mul))
			throw new InvalidOperationException("Vanilla Jungle Chests attempt budget IL pattern is missing.");
		cursor.EmitDelegate<Func<int, int>>(ContinentalStructureWorkBudget.BoundJungleChestAttempts);
	}
}
