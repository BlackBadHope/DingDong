using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchVillageCandidate(ILContext il)
	{
		Instruction nativeGeneration = il.Body.Instructions.Single(i => i.Offset == 0x01FF);
		Expect(nativeGeneration.MatchLdsflda(AbandonedVillageType, "OuterRect"), "SOTS village candidate boundary");
		ILCursor cursor = new(il);
		Instruction original = cursor.Next;
		cursor.Emit(OpCodes.Ldarga_S, il.Method.Parameters[0]);
		cursor.Emit(OpCodes.Ldarga_S, il.Method.Parameters[1]);
		cursor.Emit(OpCodes.Ldarg_2);
		cursor.Emit(OpCodes.Ldarg_3);
		cursor.EmitDelegate<SelectVillage>(TrySelectVillage);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.Emit(OpCodes.Br, nativeGeneration);
	}

	private static void PatchVillageAmbient(ILContext il)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next;
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldarg_1);
		cursor.EmitDelegate<Func<int, int, bool>>(AdmitVillageAmbient);
		cursor.Emit(OpCodes.Brtrue, original);
		cursor.Emit(OpCodes.Ret);
	}

	private static void PatchVillageConversion(ILContext il)
	{
		Instruction nextCell = il.Body.Instructions.Single(i => i.Offset == 0x0249);
		Instruction readTile = il.Body.Instructions.Single(i => i.Offset == 0x00BB);
		Expect(nextCell.MatchLdloc(2) && readTile.MatchLdsflda("Terraria.Main", "tile"),
			"SOTS village conversion cell anchors");
		ILCursor cursor = new(il);
		cursor.Goto(readTile, MoveType.Before);
		cursor.Emit(OpCodes.Ldloc_1);
		cursor.Emit(OpCodes.Ldloc_2);
		cursor.EmitDelegate<Func<int, int, bool>>(AdmitVillageConversion);
		cursor.Emit(OpCodes.Brfalse, nextCell);
	}

	private static void PatchVillageDescent(ILContext il)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(-45f),
			i => i.MatchLdcR4(45f), i => i.MatchCall("Terraria.Utils", "NextFloat")),
			"SOTS village descent steering");
		// Native tunnel shape/room generation stays intact; route stays in the admitted corridor.
		cursor.EmitDelegate<Func<float, float>>(noise => CompilerVillageSelected() ? 0f : noise);
	}

	private static void PatchVillageEntranceX(ILContext il)
	{
		ILCursor fallback = new(il);
		Expect(fallback.TryGotoNext(MoveType.AfterLabel, i => i.MatchLdloc(4),
			i => i.MatchLdfld("Terraria.DataStructures.Point16", "X")), "SOTS final native entrance selection");
		fallback.Emit(OpCodes.Ldloc, il.Body.Variables[4]);
		fallback.Emit(OpCodes.Ldloc, il.Body.Variables[7]);
		fallback.Emit(OpCodes.Ldloc_1);
		fallback.EmitDelegate<Func<Terraria.DataStructures.Point16, bool, object, Terraria.DataStructures.Point16>>(ResolveVillageEntrance);
		fallback.Emit(OpCodes.Stloc, il.Body.Variables[4]);
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(6)), "SOTS village entrance x");
		cursor.Emit(OpCodes.Ldloc, il.Body.Variables[6]);
		cursor.EmitDelegate<Func<int, int>>(x => CompilerVillageSelected() ? villageSite.X : x);
		cursor.Emit(OpCodes.Stloc, il.Body.Variables[6]);
		Expect(cursor.TryGotoNext(MoveType.After, i => i.MatchLdloc(4),
			i => i.MatchLdfld("Terraria.DataStructures.Point16", "X")), "SOTS admitted entrance x");
		cursor.EmitDelegate<Func<int, int>>(x => CompilerVillageSelected() ? villageSite.X : x);
		Expect(cursor.TryGotoNext(MoveType.After, i => i.MatchLdloc(4),
			i => i.MatchLdfld("Terraria.DataStructures.Point16", "Y")), "SOTS admitted entrance y");
		cursor.EmitDelegate<Func<int, int>>(y => CompilerVillageSelected() ? villageSite.Ground : y);
		Expect(cursor.TryGotoNext(MoveType.Before, i => i.MatchRet()), "SOTS village completion");
		cursor.EmitDelegate<Action>(LogVillageProtection);
	}
}
