using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

internal static class RandomTileBudgetPatches
{
	private readonly record struct Site(Instruction Multiply, int CoefficientLocal, Code Conversion);

	internal static void Vanilla(ILContext il) => Apply(il,
		new[] { 3, 5, 4 }, new[] { Code.Conv_R8, Code.Conv_R8, Code.Conv_R8 });

	internal static void Calamity(ILContext il) => Apply(il,
		new[] { 1, 3, 2, 12 }, new[] { Code.Conv_R8, Code.Conv_R8, Code.Conv_R8, Code.Conv_R4 });

	private static void Apply(ILContext il, int[] coefficientLocals, Code[] conversions)
	{
		var instructions = il.Body.Instructions;
		List<Site> sites = new();
		int rawAreaExpressions = 0;
		for (int i = 0; i + 2 < instructions.Count; i++)
		{
			if (!IsDimension(instructions[i], "maxTilesX") ||
				!IsDimension(instructions[i + 1], "maxTilesY") || instructions[i + 2].OpCode != OpCodes.Mul)
				continue;
			rawAreaExpressions++;
			if (i + 5 >= instructions.Count ||
				instructions[i + 3].OpCode.Code is not (Code.Conv_R8 or Code.Conv_R4) ||
				Local(instructions[i + 4]) < 0 || instructions[i + 5].OpCode != OpCodes.Mul)
				continue;
			sites.Add(new Site(instructions[i + 2], Local(instructions[i + 4]), instructions[i + 3].OpCode.Code));
		}
		// Validate the whole contract before changing one instruction. Other users of
		// physical dimensions (RNG bounds, tile-map access, events) are left intact.
		if (rawAreaExpressions != coefficientLocals.Length || sites.Count != coefficientLocals.Length)
			throw new InvalidOperationException($"Random tile budget shape changed in {il.Method.FullName}: {rawAreaExpressions}/{sites.Count}.");
		for (int i = 0; i < sites.Count; i++)
			if (sites[i].CoefficientLocal != coefficientLocals[i] || sites[i].Conversion != conversions[i])
				throw new InvalidOperationException($"Random tile budget coefficient/precision changed in {il.Method.FullName}, site {i}.");
		foreach (Site site in sites)
		{
			ILCursor cursor = new(il) { Index = instructions.IndexOf(site.Multiply) + 1 };
			cursor.EmitDelegate<Func<int, int>>(NormalizeConfiguredArea);
		}
	}

	private static int NormalizeConfiguredArea(int nativeArea) => RandomTileBudgetPolicy.NormalizeArea(nativeArea,
		ModContent.GetInstance<RandomTileBudgetConfig>().AutoNormalizeExpandedWorldGrowth);

	private static bool IsDimension(Instruction instruction, string name) =>
		instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.Main" && field.Name == name &&
		field.FieldType.FullName == "System.Int32";

	private static int Local(Instruction instruction) => instruction.OpCode.Code switch
	{
		Code.Ldloc_0 => 0, Code.Ldloc_1 => 1, Code.Ldloc_2 => 2, Code.Ldloc_3 => 3,
		Code.Ldloc or Code.Ldloc_S when instruction.Operand is VariableDefinition variable => variable.Index,
		_ => -1
	};
}
