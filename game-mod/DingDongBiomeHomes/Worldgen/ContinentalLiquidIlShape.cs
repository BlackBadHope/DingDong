using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalLiquidIlShape
{
	// Validate before emitting anything. The native block is retained as the
	// fallback for vanilla-sized worlds and generation, not removed or rewritten.
	internal static (int Start, int End) FindRefill(IList<Instruction> code)
	{
		int start = -1, remove = -1, count = 0;
		for (int i = 0; i + 5 < code.Count; i++)
		{
			if (Field(code[i], "curMaxLiquid") && Field(code[i + 1], "curMaxLiquid") &&
				Field(code[i + 2], "numLiquid") && code[i + 3].OpCode == OpCodes.Sub &&
				code[i + 4].OpCode == OpCodes.Sub && Local(code[i + 5], true) == 13)
			{ start = i; count++; }
		}
		if (count != 1) throw new InvalidOperationException("Liquid refill budget shape changed.");
		count = 0;
		for (int i = start; i < code.Count; i++)
			if (Call(code[i], "Terraria.LiquidBuffer", "DelBuffer")) { remove = i; count++; }
		if (count != 1 || remove + 7 >= code.Count ||
			code[remove - 1].OpCode != OpCodes.Ldc_I4_0 ||
			Local(code[remove + 1], false) != 15 || code[remove + 2].OpCode != OpCodes.Ldc_I4_1 ||
			code[remove + 3].OpCode != OpCodes.Add || Local(code[remove + 4], true) != 15 ||
			Local(code[remove + 5], false) != 15 || Local(code[remove + 6], false) != 13 ||
			!(code[remove + 7].OpCode == OpCodes.Blt || code[remove + 7].OpCode == OpCodes.Blt_S))
			throw new InvalidOperationException("Liquid refill removal loop changed.");
		int addCalls = 0, clearCalls = 0;
		for (int i = start; i < remove; i++)
		{
			if (Call(code[i], "Terraria.Liquid", "AddWater")) addCalls++;
			if (Call(code[i], "Terraria.Tile", "checkingLiquid")) clearCalls++;
		}
		if (addCalls != 1 || clearCalls != 1)
			throw new InvalidOperationException("Liquid refill admission ABI changed.");
		return (start, remove + 8);
	}

	private static bool Field(Instruction i, string name) => i.OpCode == OpCodes.Ldsfld &&
		i.Operand is FieldReference f && f.DeclaringType.FullName == "Terraria.Liquid" && f.Name == name;
	private static bool Call(Instruction i, string type, string name) => i.OpCode == OpCodes.Call &&
		i.Operand is MethodReference m && m.DeclaringType.FullName == type && m.Name == name;
	private static int Local(Instruction i, bool store) =>
		i.OpCode == (store ? OpCodes.Stloc_S : OpCodes.Ldloc_S) && i.Operand is VariableDefinition v ? v.Index : -1;
}
