using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchOres(ILContext il)
	{
		ILCursor target = new(il);
		Expect(target.TryGotoNext(MoveType.After, i => i.MatchLdcR4(480f)), "SOTS ore target");
		target.EmitDelegate<Func<float, float>>(ContinentalAdapterMath.ScaleSotsOreTarget);
		PatchAttemptBudget(il, ContinentalAdapterMath.BoundSotsOreAttempts, "SOTS ore attempts");
		PatchOreSample(il);
		PatchReceipts<float>(il, SotsWorldgenReceipt.ValidateOres, 2);
	}

	private static void PatchGeodes(ILContext il)
	{
		ILCursor target = new(il);
		Expect(target.TryGotoNext(MoveType.After,
			i => i.MatchLdcI4(120), i => i.MatchStloc(0)), "SOTS geode target");
		target.Index--;
		target.EmitDelegate<Func<int, int>>(ContinentalAdapterMath.ScaleSotsGeodeTarget);
		PatchAttemptBudget(il, ContinentalAdapterMath.BoundSotsGeodeAttempts, "SOTS geode attempts");
		PatchReceipts<int>(il, SotsWorldgenReceipt.ValidateGeodes, 1);
	}

	private static void PatchAttemptBudget(ILContext il, Func<int, int> bound, string label)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After,
			IsMainField("maxTilesX"), IsMainField("maxTilesY"),
			i => i.OpCode == OpCodes.Mul, i => i.OpCode == OpCodes.Conv_R4,
			i => i.MatchLdcR4(0.2f), i => i.OpCode == OpCodes.Mul,
			i => i.OpCode == OpCodes.Conv_I4), label);
		cursor.EmitDelegate(bound);
	}

	private static void PatchOreSample(ILContext il)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After,
			i => i.MatchStloc(2), i => i.MatchLdloc(2),
			i => i.MatchCall("SOTS.WorldgenHelpers.SOTSWorldgenHelper", "GenerateFrigidIceOre")),
			"SOTS ore snow-row sample");
		cursor.Index -= 2;
		cursor.Emit(OpCodes.Ldloc_2);
		cursor.EmitDelegate<Func<int, int, int>>(ContinentalAdapterMath.RemapSotsOreX);
	}

	private static void PatchReceipts<T>(ILContext il, Action<T> validator, int expected)
	{
		Instruction[] returns = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
		Expect(returns.Length == expected, $"SOTS terminal count {returns.Length}/{expected}");
		foreach (Instruction terminal in returns)
		{
			ILCursor cursor = new(il);
			// AfterLabel keeps incoming branches on the validator. Inserting with
			// MoveType.Before leaves branches targeted at the old ret instruction,
			// so the successful early-exit path can bypass the receipt entirely.
			cursor.Goto(terminal, MoveType.AfterLabel);
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.EmitDelegate(validator);
		}
	}

	private static Func<Instruction, bool> IsMainField(string name) => instruction =>
		instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.Main" && field.Name == name;

	private static void PatchTopaz(ILContext il) =>
		EmitContinentalOverride(il, SotsGemPlacement.PlaceTopaz);

	private static void PatchSapphire(ILContext il) =>
		EmitContinentalOverride(il, SotsGemPlacement.PlaceSapphire);

	private static void EmitContinentalOverride(ILContext il, Action replacement)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next ?? throw new InvalidOperationException("SOTS method is empty.");
		cursor.EmitDelegate<Func<bool>>(ContinentalAdapterMath.Active);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.EmitDelegate<Action>(replacement);
		cursor.Emit(OpCodes.Ret);
	}

	private static void Expect(bool found, string label)
	{
		if (!found)
			throw new InvalidOperationException($"Exact IL pattern missing: {label}.");
	}
}
