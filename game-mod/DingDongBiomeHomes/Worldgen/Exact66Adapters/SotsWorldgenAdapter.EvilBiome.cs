using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchFlattenEvilBiomeIndex(ILContext il)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next ??
			throw new InvalidOperationException("SOTS FlattenEvilBiome is empty.");
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<int, bool>>(AdmitEvilBiomeIndex);
		cursor.Emit(OpCodes.Brtrue, original);
		cursor.Emit(OpCodes.Ret);
	}

	private static bool AdmitEvilBiomeIndex(int index)
	{
		bool valid = index >= 0;
		if (ContinentalAdapterMath.Active())
			SotsWorldgenReceipt.MarkEvilBiomeGuard(valid);
		return valid;
	}
}
