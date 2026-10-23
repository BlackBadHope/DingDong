using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchDungeonArchiveTileLookup(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.Before,
			i => IsMainField(i, "tileDungeon"),
			IsLocalAddress,
			IsTileTypeGetter,
			IsUnsigned16Dereference,
			i => i.OpCode == OpCodes.Ldelem_U1))
			throw new InvalidOperationException(
				"Calamity Dungeon Archive tileDungeon lookup IL pattern is missing.");

		cursor.Index += 4;
		cursor.Remove();
		cursor.EmitDelegate<Func<bool[], int, bool>>(SafeDungeonTileLookup);

		ILCursor floor = new(il);
		if (!floor.TryGotoNext(MoveType.Before,
			IsLocalLoad,
			i => i.MatchLdcI4(0),
			i => i.OpCode == OpCodes.Bgt || i.OpCode == OpCodes.Bgt_S))
			throw new InvalidOperationException(
				"Calamity Dungeon Archive vertical floor IL pattern is missing.");
		floor.Index++;
		floor.Remove();
		floor.Emit(OpCodes.Ldc_I4_5);
	}

	private static bool SafeDungeonTileLookup(bool[] lookup, int tileType)
	{
		bool inRange = tileType >= 0 && tileType < lookup.Length;
		if (ContinentalAdapterMath.Active())
			CalamityWorldgenReceipt.MarkArchiveLookup(!inRange);
		return inRange && lookup[tileType];
	}

	private static bool IsLocalLoad(Instruction instruction) =>
		instruction.OpCode == OpCodes.Ldloc ||
		instruction.OpCode == OpCodes.Ldloc_S ||
		instruction.OpCode == OpCodes.Ldloc_0 ||
		instruction.OpCode == OpCodes.Ldloc_1 ||
		instruction.OpCode == OpCodes.Ldloc_2 ||
		instruction.OpCode == OpCodes.Ldloc_3;

	private static bool IsLocalAddress(Instruction instruction) =>
		instruction.OpCode == OpCodes.Ldloca ||
		instruction.OpCode == OpCodes.Ldloca_S;

	private static bool IsUnsigned16Dereference(Instruction instruction) =>
		instruction.OpCode == OpCodes.Ldind_U2;

	private static bool IsTileTypeGetter(Instruction instruction) =>
		(instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
		instruction.Operand is MethodReference method &&
		method.DeclaringType.FullName == "Terraria.Tile" &&
		method.Name == "get_TileType";
}
