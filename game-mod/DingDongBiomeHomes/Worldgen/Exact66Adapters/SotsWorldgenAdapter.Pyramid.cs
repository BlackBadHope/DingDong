using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchPyramidWidthTable(ILContext il)
	{
		ILCursor cursor = new(il);
		foreach (int threshold in new[] { 4000, 6000, 8000 })
		{
			if (!cursor.TryGotoNext(MoveType.After, IsMaxTilesX) ||
				cursor.Next == null || !cursor.Next.MatchLdcI4(threshold))
				throw new InvalidOperationException(
					$"SOTS pyramid width threshold {threshold} IL pattern is missing.");
			cursor.EmitDelegate<Func<int, int>>(BoundPyramidWidth);
		}
	}

	private static int BoundPyramidWidth(int width)
	{
		int bounded = ContinentalStructureWorkBudget.BoundSotsLegacyWidth(width);
		if (ContinentalAdapterMath.Active()) SotsWorldgenReceipt.MarkPyramid(bounded);
		return bounded;
	}

	private static bool IsMaxTilesX(Instruction instruction) =>
		instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
		field.DeclaringType.FullName == "Terraria.Main" && field.Name == "maxTilesX";
}
