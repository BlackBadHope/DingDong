#nullable enable
using System;
using System.Diagnostics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static Stopwatch? mushroomStageClock;

	private static void PatchMushroomPatchesWorkBudget(ILContext il)
	{
		PatchMushroomExclusion(il);
		ILCursor start = new(il);
		start.EmitDelegate<Action>(BeginMushroomStageProfile);

		ILCursor attempts = new(il);
		if (!attempts.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(2),
			i => i.OpCode == OpCodes.Div, i => i.OpCode == OpCodes.Bgt_S))
			throw new InvalidOperationException(
				"Vanilla mushroom-patch retry budget IL pattern is missing.");
		attempts.Index--;
		attempts.EmitDelegate<Func<int, int>>(BoundMushroomPatchAttempts);

		ILCursor paint = new(il);
		if (!paint.TryGotoNext(MoveType.Before,
			i => i.MatchLdcI4(0), i => i.MatchStloc(16)))
			throw new InvalidOperationException(
				"Vanilla mushroom-patch paint stage IL pattern is missing.");
		paint.EmitDelegate<Action>(MarkMushroomPaintStage);

		ILCursor finish = new(il);
		if (!finish.TryGotoNext(MoveType.Before, i => i.OpCode == OpCodes.Ret))
			throw new InvalidOperationException(
				"Vanilla mushroom-patch return IL pattern is missing.");
		finish.EmitDelegate<Action>(FinishMushroomStageProfile);
	}

	private static int BoundMushroomPatchAttempts(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, MushroomWorkBudget().AttemptsPerBiome) : original;

	private static ContinentalMushroomWorkBudget MushroomWorkBudget() =>
		Compiler.Runtime.WorldCompilerActivation.IsActive
			? ContinentalMushroomWorkBudget.Create(Main.maxTilesX, ContinentalWorldDimensions.VanillaLargeWidth)
			: ContinentalMushroomWorkBudget.CreateNative(Main.maxTilesX);

	private static void BeginMushroomStageProfile()
	{
		if (!ContinentalAdapterMath.Active()) return;
		emptyMushroomRootWindows = 0;
		CalamityWorldgenReceipt.MarkMushroomBudget(
			MushroomWorkBudget().AttemptsPerBiome);
		if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(
			"DINGDONG_X10_RUNTIME_RUN_ID"))) return;
		mushroomStageClock = Stopwatch.StartNew();
		WriteMushroomStage("search");
	}

	private static void MarkMushroomPaintStage() => WriteMushroomStage("paint");

	private static void FinishMushroomStageProfile()
	{
		if (ContinentalAdapterMath.Active())
			Console.WriteLine($"DINGDONG_MUSHROOM_ROOTS|emptyOptionalWindows={emptyMushroomRootWindows}|searchBound=800|parentBiomeSkipped=False");
		WriteMushroomStage("end");
		mushroomStageClock = null;
	}

	private static void WriteMushroomStage(string stage)
	{
		if (mushroomStageClock is null) return;
		Console.WriteLine($"DINGDONG_MUSHROOM_STAGE|stage={stage}|" +
			$"elapsedMs={mushroomStageClock.ElapsedMilliseconds}");
	}
}
