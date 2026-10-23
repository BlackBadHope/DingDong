using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static int dungeonStairSegments;

	private static void PatchDungeonStairBudget(ILContext il)
	{
		ILCursor stairs = new(il);
		if (!stairs.TryGotoNext(MoveType.After,
			i => i.MatchLdcI4(0),
			i => IsGenVarsField(i, "dSurface") && i.OpCode == OpCodes.Stsfld))
			throw new InvalidOperationException(
				"Vanilla dungeon stair-loop initialization IL pattern is missing.");
		stairs.EmitDelegate<Action>(BeginDungeonStairGuard);
		EmitDungeonStage(stairs, "stairs");

		ILCursor stairStep = new(il);
		if (!stairStep.TryGotoNext(MoveType.After,
			i => IsWorldGenCall(i, "DungeonStairs")))
			throw new InvalidOperationException(
				"Vanilla dungeon stair-step IL pattern is missing.");
		stairStep.EmitDelegate<Action>(GuardDungeonStairProgress);

		ILCursor entrance = new(il);
		if (!entrance.TryGotoNext(MoveType.Before,
			i => IsWorldGenCall(i, "DungeonEnt")))
			throw new InvalidOperationException(
				"Vanilla dungeon entrance IL pattern is missing.");
		EmitDungeonStage(entrance, "entrance");
	}

	private static void BeginDungeonStairGuard() => dungeonStairSegments = 0;

	private static void GuardDungeonStairProgress()
	{
		if (!ContinentalAdapterMath.Active()) return;
		dungeonStairSegments++;
		if (!GenVars.dSurface && dungeonStairSegments >= WorkBudget().StairSegmentAttempts)
			throw new InvalidOperationException(
				$"Dungeon stairs did not reach the surface after {dungeonStairSegments} segments. " +
				"The surface flag was not fabricated; inspect the native dungeon stage journal.");
	}

	private static bool IsGenVarsField(Instruction instruction, string name) =>
		instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.WorldBuilding.GenVars" &&
		field.Name == name;

	private static bool IsWorldGenCall(Instruction instruction, string name) =>
		instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method &&
		method.DeclaringType.FullName == "Terraria.WorldGen" && method.Name == name;
}
