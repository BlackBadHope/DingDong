using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchFullDesertAttemptBudget(ILContext il)
	{
		ILCursor start = new(il);
		start.EmitDelegate<Action>(MarkFullDesertAttemptBudget);

		ILCursor attempts = new(il);
		if (!attempts.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(4),
			i => i.OpCode == OpCodes.Div, i => i.OpCode == OpCodes.Ble_S))
			throw new InvalidOperationException(
				"Vanilla full-desert retry budget IL pattern is missing.");
		attempts.Index--;
		attempts.EmitDelegate<Func<int, int>>(BoundFullDesertAttempts);
	}

	private static int BoundFullDesertAttempts(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, DesertWorkBudget().AttemptsPerSide) : original;

	private static void MarkFullDesertAttemptBudget()
	{
		if (ContinentalAdapterMath.Active())
			CalamityWorldgenReceipt.MarkDesertBudget(
				DesertWorkBudget().AttemptsPerSide);
	}

	private static ContinentalDesertWorkBudget DesertWorkBudget() =>
		Compiler.Runtime.WorldCompilerActivation.IsActive
			? ContinentalDesertWorkBudget.Create(Main.maxTilesX, ContinentalWorldDimensions.VanillaLargeWidth)
			: ContinentalDesertWorkBudget.CreateNative(Main.maxTilesX);
}
