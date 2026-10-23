using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchVanillaTempleBudget(ILContext il)
	{
		ILCursor paths = new(il);
		if (!paths.TryGotoNext(MoveType.AfterLabel,
			i => i.MatchLdcI4(1), i => i.MatchStloc(3)))
			throw new InvalidOperationException("Vanilla temple post-seed room count anchor is missing.");
		EmitTempleCapacity(paths, il, 2);

		ILCursor rooms = new(il);
		if (!rooms.TryGotoNext(MoveType.Before, i => i.MatchStloc(2),
			i => IsWorldGenField(i, "drunkWorldGen")))
			throw new InvalidOperationException("Vanilla temple base-room budget IL pattern is missing.");
		rooms.EmitDelegate<Func<int, int>>(ContinentalStructureWorkBudget.BoundTempleBaseRooms);

		ILCursor scale = new(il);
		if (!scale.TryGotoNext(MoveType.After, i => IsMainField(i, "maxTilesX"),
			i => i.OpCode == OpCodes.Conv_R8, i => i.MatchLdcR8(4200d),
			i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Vanilla temple scale IL pattern is missing.");
		scale.EmitDelegate<Func<double, double>>(ContinentalStructureWorkBudget.BoundTempleScale);
	}

	private static void PatchCalamityTempleBudget(ILContext il)
	{
		ILCursor paths = new(il);
		if (!paths.TryGotoNext(MoveType.AfterLabel,
			i => IsWorldGenCall(i, "get_genRand"), i => i.MatchLdcI4(2),
			i => i.Operand is MethodReference method && method.Name == "NextBool" &&
				method.DeclaringType.FullName == "Terraria.Utils"))
			throw new InvalidOperationException("Calamity temple post-seed room count anchor is missing.");
		EmitTempleCapacity(paths, il, 1);

		ILCursor rooms = new(il);
		if (!rooms.TryGotoNext(MoveType.Before, i => i.MatchStloc(1),
			i => IsWorldGenField(i, "getGoodWorldGen")))
			throw new InvalidOperationException("Calamity temple base-room budget IL pattern is missing.");
		rooms.EmitDelegate<Func<int, int>>(BoundCalamityTempleBaseRooms);

		ILCursor scale = new(il);
		if (!scale.TryGotoNext(MoveType.After, i => IsMainField(i, "maxTilesX"),
			i => i.OpCode == OpCodes.Conv_R4, i => i.MatchLdcR4(4200f),
			i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Calamity temple scale IL pattern is missing.");
		scale.EmitDelegate<Func<float, float>>(BoundCalamityTempleScale);
	}

	private static float BoundCalamityTempleScale(float original)
	{
		float bounded = ContinentalStructureWorkBudget.BoundTempleScale(original);
		if (ContinentalAdapterMath.Active()) CalamityWorldgenReceipt.MarkTempleScale(bounded);
		return bounded;
	}

	private static void EmitTempleCapacity(ILCursor cursor, ILContext il, int roomsLocal)
	{
		if (il.Body.Variables[0].VariableType.FullName != "Microsoft.Xna.Framework.Rectangle[]" ||
			il.Body.Variables[roomsLocal].VariableType.FullName != "System.Int32")
			throw new InvalidOperationException("Temple local variable layout changed.");
		cursor.Emit(OpCodes.Ldloc_0);
		cursor.Emit(OpCodes.Ldloc, il.Body.Variables[roomsLocal]);
		cursor.EmitDelegate<Func<Rectangle[], int, Rectangle[]>>((paths, rooms) =>
			ContinentalAdapterMath.Active() ? NativeTemplePathCapacity.Ensure(paths, rooms) : paths);
		cursor.Emit(OpCodes.Stloc_0);
	}

	private static int BoundCalamityTempleBaseRooms(int original)
	{
		int bounded = ContinentalStructureWorkBudget.BoundTempleBaseRooms(original);
		if (ContinentalAdapterMath.Active()) CalamityWorldgenReceipt.MarkTempleBaseRooms(bounded);
		return bounded;
	}

	private static bool IsWorldGenField(Instruction instruction, string name) =>
		instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.WorldGen" && field.Name == name;
}
