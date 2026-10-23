using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private const string DesignateEvilBiomeHash =
		"2590904DA5B5B45F8D8865029448D3461D8C3C6B4F4763A98B8E2E6C4DE04B7E";
	private const string FillAbandonedVillageChestsHash =
		"C0144C1E668F33AFD01644F13954EE88BC542DCDFFB948CCB808FF4C56362FF0";
	private static MethodInfo flattenEvilBiomeMethod;
	private static Type corruptionRectangleType;
	private static FieldInfo corruptionRectangleField;
	private static FieldInfo corruptionAverageHeightField;

	private static void InstallAbandonedVillageCompatibility(Mod provider,
		MethodInfo flattenEvilBiome, List<IDisposable> hooks)
	{
		MethodInfo designate = Exact66ProviderGate.Method(provider,
			AbandonedVillageType, "DesignateDesiredEvilBiome", true, typeof(void));
		MethodInfo fill = Exact66ProviderGate.Method(provider, AbandonedVillageType,
			"FillChestsWithLoot", true, typeof(void));
		Exact66ProviderGate.AssertIl(designate, DesignateEvilBiomeHash);
		Exact66ProviderGate.AssertIl(fill, FillAbandonedVillageChestsHash);
		flattenEvilBiomeMethod = flattenEvilBiome;
		corruptionRectangleType = corruptionsField?.FieldType.GetGenericArguments()[0] ??
			throw new InvalidOperationException("SOTS corruption rectangle type is unavailable.");
		corruptionRectangleField = RequireField(corruptionRectangleType, "rect");
		corruptionAverageHeightField = RequireField(corruptionRectangleType,
			"AverageHeight");
		hooks.Add(new ILHook(designate, PatchCompilerEvilBiome));
		hooks.Add(new ILHook(fill, PatchAbandonedVillageLoot));
	}

	private static FieldInfo RequireField(Type type, string name) =>
		type.GetField(name, BindingFlags.Instance | BindingFlags.Public |
			BindingFlags.NonPublic) ?? throw new MissingFieldException(type.FullName, name);

	private static void PatchCompilerEvilBiome(ILContext il)
	{
		ILCursor cursor = new(il);
		Expect(cursor.TryGotoNext(MoveType.After,
			i => i.MatchCall(AbandonedVillageType, "InitializeValidTileLists")),
			"SOTS compiler evil bridge anchor");
		Instruction original = cursor.Next ??
			throw new InvalidOperationException("SOTS evil designation body is truncated.");
		cursor.EmitDelegate<Func<bool>>(TryBuildCompilerEvilBiome);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.Emit(OpCodes.Ret);
	}

}
