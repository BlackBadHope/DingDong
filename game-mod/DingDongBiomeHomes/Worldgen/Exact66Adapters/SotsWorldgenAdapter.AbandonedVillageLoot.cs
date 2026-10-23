using System;
using System.Collections;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static void PatchAbandonedVillageLoot(ILContext il)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After,
			i => i.MatchCall(AbandonedVillageType, "BestEvilBiome"),
			i => i.MatchStloc(0)), "SOTS abandoned village loot index");
		Instruction original = cursor.Next ??
			throw new InvalidOperationException("SOTS village loot body is truncated.");
		cursor.Emit(OpCodes.Ldloc_0);
		cursor.EmitDelegate<Func<int, bool>>(AdmitAbandonedVillageLootIndex);
		cursor.Emit(OpCodes.Brtrue, original);
		cursor.Emit(OpCodes.Ret);
	}

	private static bool AdmitAbandonedVillageLootIndex(int index)
	{
		if (!ContinentalAdapterMath.Active()) return true;
		if (corruptionsField?.GetValue(null) is not ICollection corruptions)
			throw new InvalidOperationException("SOTS corruption list is unavailable.");
		bool valid = (uint)index < (uint)corruptions.Count;
		SotsWorldgenReceipt.MarkAbandonedVillageLootGuard(valid);
		if (!valid)
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Error(
				$"DINGDONG_SOTS_VILLAGE_LOOT|verdict=WITHHELD|index={index}" +
				$"|count={corruptions.Count}|mutated=0");
		return valid;
	}
}
