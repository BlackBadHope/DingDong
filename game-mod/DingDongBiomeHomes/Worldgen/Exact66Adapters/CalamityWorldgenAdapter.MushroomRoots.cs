using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private delegate bool SelectMushroomRoot(Vector2D center, ref int x, ref int y);
	private static long emptyMushroomRootWindows;

	private static void PatchMushroomRootSearch(ILContext il)
	{
		PatchMushroomGeometry(il);
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.Before,
			i => i.OpCode == OpCodes.Ldsflda && i.Operand is FieldReference field &&
				field.DeclaringType.FullName == "Terraria.Main" && field.Name == "tile",
			i => i.MatchLdloc(17), i => i.MatchLdloc(18)))
			throw new InvalidOperationException("ShroomPatch root predicate is missing.");
		Instruction predicate = cursor.Next;
		ILCursor accepted = new(il);
		accepted.Goto(predicate);
		if (!accepted.TryGotoNext(MoveType.After,
			i => i.MatchLdcI4(59), i => i.OpCode == OpCodes.Bne_Un_S || i.OpCode == OpCodes.Bne_Un))
			throw new InvalidOperationException("ShroomPatch root retry branch is missing.");
		Instruction paint = accepted.Next;
		if (!accepted.TryGotoNext(MoveType.Before,
			i => i.MatchLdloc(16), i => i.MatchLdcI4(1),
			i => i.OpCode == OpCodes.Add, i => i.MatchStloc(16)))
			throw new InvalidOperationException("ShroomPatch root continuation is missing.");
		Instruction nextRoot = accepted.Next;
		cursor.Goto(predicate, MoveType.AfterLabel);
		cursor.EmitDelegate<Func<bool>>(ContinentalAdapterMath.Active);
		cursor.Emit(OpCodes.Brfalse, predicate);
		cursor.Emit(OpCodes.Ldloc, il.Body.Variables[5]);
		cursor.Emit(OpCodes.Ldloca, il.Body.Variables[17]);
		cursor.Emit(OpCodes.Ldloca, il.Body.Variables[18]);
		cursor.EmitDelegate<SelectMushroomRoot>(TrySelectMushroomRoot);
		cursor.Emit(OpCodes.Brtrue, paint);
		cursor.Emit(OpCodes.Br, nextRoot);
	}

	private static bool TrySelectMushroomRoot(Vector2D center, ref int x, ref int y)
	{
		bool found = BoundedMushroomRootSearch.TrySelect((int)center.X,
			(int)center.Y, Main.maxTilesX, Main.maxTilesY,
			MushroomRootEligible, count => WorldGen.genRand.Next(count), ref x, ref y);
		if (!found) emptyMushroomRootWindows++;
		return found;
	}

	private static bool MushroomRootEligible(int x, int y) =>
		Main.tile[x, y].HasTile || Main.tile[x, y].TileType == Terraria.ID.TileID.Mud;
}
