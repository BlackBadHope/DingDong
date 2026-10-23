using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchDungeonTrapTarget(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(500),
			i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Vanilla dungeon trap-target IL pattern is missing.");
		cursor.EmitDelegate<Func<int, int>>(BoundTrapTarget);
		EmitDungeonStage(cursor, "traps");
	}

	private static void PatchDungeonLightTarget(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => IsMainField(i, "maxTilesX"), i => i.MatchLdcI4(150),
			i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Vanilla dungeon light-target IL pattern is missing.");
		cursor.EmitDelegate<Func<int, int>>(BoundLightTarget);
		EmitDungeonStage(cursor, "lights");
	}

	private static void PatchDungeonGroundFurnitureAttempts(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => i.MatchLdcI4(2000), i => IsMainField(i, "maxTilesX"),
			i => i.OpCode == OpCodes.Mul, i => i.OpCode == OpCodes.Conv_R8,
			i => i.MatchLdcR8(4200d), i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Vanilla dungeon ground-furniture budget IL pattern is missing.");
		cursor.EmitDelegate<Func<double, double>>(BoundGroundFurnitureAttempts);
		EmitDungeonStage(cursor, "ground-furniture");
	}

	private static int BoundTrapTarget(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().TrapTarget) : original;

	private static int BoundLightTarget(int original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().LightTarget) : original;

	private static double BoundGroundFurnitureAttempts(double original) =>
		ContinentalAdapterMath.Active()
			? Math.Min(original, WorkBudget().GroundFurnitureAttempts) : original;
}
