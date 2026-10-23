using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private const string AbandonedMinesHash =
		"7E2B5E9AA8DF7CEE1C97EDDCEBDE3183BC8C7D9C5D73233C042D2DEAFD46611E";
	private const string AbandonedVillageType =
		"SOTS.WorldgenHelpers.AbandonedVillageWorldgenHelper";
	private static FieldInfo corruptionsField;

	private static void InstallAbandonedMineGuard(Mod provider,
		List<IDisposable> hooks)
	{
		MethodInfo mines = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"GenerateAbandonedMinesInsideTheCorruptionRectangle", true, typeof(void));
		Type helper = mines.DeclaringType ?? throw new MissingMemberException(
			AbandonedVillageType);
		corruptionsField = helper.GetField("Corruptions", BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic) ??
			throw new MissingFieldException(AbandonedVillageType, "Corruptions");
		Exact66ProviderGate.AssertIl(mines, AbandonedMinesHash);
		hooks.Add(new ILHook(mines, PatchAbandonedMineIndex));
		hooks.Add(new ILHook(mines, PatchVillageEntranceX));
	}

	private static void PatchAbandonedMineIndex(ILContext il)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After,
			i => i.MatchCall(AbandonedVillageType, "BestEvilBiome"),
			i => i.MatchStloc(0)), "SOTS abandoned mine biome index");
		Instruction original = cursor.Next ??
			throw new InvalidOperationException("SOTS abandoned mine body is truncated.");
		cursor.Emit(OpCodes.Ldloc_0);
		cursor.EmitDelegate<Func<int, bool>>(AdmitAbandonedMineIndex);
		cursor.Emit(OpCodes.Brtrue, original);
		cursor.Emit(OpCodes.Ret);
	}

	private static bool AdmitAbandonedMineIndex(int index)
	{
		if (!ContinentalAdapterMath.Active()) return true;
		if (corruptionsField?.GetValue(null) is not ICollection corruptions)
			throw new InvalidOperationException(
				"SOTS corruption rectangle collection is unavailable.");
		bool valid = (uint)index < (uint)corruptions.Count;
		SotsWorldgenReceipt.MarkAbandonedMineGuard(valid, index, corruptions.Count);
		return valid;
	}

	private static void ResetAbandonedMineGuard()
	{
		corruptionsField = null;
		ResetAbandonedVillageCompatibility();
	}
}
