using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ThoriumWorldgenAdapter
{
	private const string TypeName = "ThoriumMod.WorldGeneration.Depths.AquaticDepthsBiomePass";
	private const string RadiusHash = "4C4BD4FEBBA524C9ED0CEBA8BE908B27AAB68F91A51EC0761EBA015D89141E97";
	private const string BoundsHash = "F5493EA194C9A0EDAE48915499255D4B9C57B903DE7F5FFC23CC12B8B2F538EC";
	private const string OceanScanHash = "7667B743FB130D60E269BE768F13D6B717E64D57A3728A0461622558D4604754";

	internal static void Install(Mod provider, List<IDisposable> hooks)
	{
		MethodInfo radius = Exact66ProviderGate.Method(provider, TypeName, "Scan_SelectEliptic", false,
			typeof(void), typeof(Vector2), typeof(Vector2), typeof(float), typeof(float), typeof(float));
		MethodInfo bounds = Exact66ProviderGate.Method(provider, TypeName, "PostStep_1_CalculateBounds",
			false, typeof(void));
		MethodInfo oceanScan = Exact66ProviderGate.Method(provider, TypeName, "Scan_AvoidBeach",
			false, typeof(void), typeof(int), typeof(int));
		Exact66ProviderGate.AssertIl(radius, RadiusHash);
		Exact66ProviderGate.AssertIl(bounds, BoundsHash);
		Exact66ProviderGate.AssertIl(oceanScan, OceanScanHash);
		Type passType = provider.Code.GetType(TypeName, true);
		ThoriumBoundsCalculator.Bind(passType);
		ThoriumOceanFloodFill.Bind(passType);
		hooks.Add(new Exact66ActionDisposable(ThoriumBoundsCalculator.Reset));
		hooks.Add(new Exact66ActionDisposable(ThoriumOceanFloodFill.Reset));
		hooks.Add(new ILHook(radius, PatchRadius));
		hooks.Add(new ILHook(bounds, PatchBounds));
		hooks.Add(new ILHook(oceanScan, PatchOceanScan));
	}

	private static void PatchRadius(ILContext il)
	{
		ILCursor cursor = new(il);
		for (int match = 0; match < 3; match++)
		{
			if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(130f)))
				throw new InvalidOperationException($"Thorium radius IL occurrence {match + 1}/3 is missing.");
			cursor.EmitDelegate<Func<float, float>>(ContinentalAdapterMath.ScaleThoriumRadius);
		}
		if (cursor.TryGotoNext(i => i.MatchLdcR4(130f)))
			throw new InvalidOperationException("Thorium radius IL has an unexpected fourth occurrence.");
	}

	private static void PatchBounds(ILContext il)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next ?? throw new InvalidOperationException("Thorium bounds method is empty.");
		cursor.EmitDelegate<Func<bool>>(ContinentalAdapterMath.Active);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Action<object>>(ThoriumBoundsCalculator.Calculate);
		cursor.Emit(OpCodes.Ret);
	}

	private static void PatchOceanScan(ILContext il)
	{
		ILCursor cursor = new(il);
		Instruction original = cursor.Next ?? throw new InvalidOperationException("Thorium ocean scan method is empty.");
		cursor.EmitDelegate<Func<bool>>(ContinentalAdapterMath.Active);
		cursor.Emit(OpCodes.Brfalse, original);
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldarg_1);
		cursor.Emit(OpCodes.Ldarg_2);
		cursor.EmitDelegate<Action<object, int, int>>(ThoriumOceanFloodFill.Scan);
		cursor.Emit(OpCodes.Ret);
	}
}
