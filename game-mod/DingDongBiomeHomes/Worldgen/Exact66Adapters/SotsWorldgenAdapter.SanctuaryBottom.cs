using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private const string SanctuaryBottomHash =
		"68EF7CF8A8AD75014289794EA9605AE752045916E31DD7AC3C8217ED4888FF4B";

	private static void InstallSanctuaryBottomCallSites(Mod provider,
		List<IDisposable> hooks)
	{
		var bottom = Exact66ProviderGate.Method(provider, SanctuaryType,
			"get_Bottom", true, typeof(int));
		Exact66ProviderGate.AssertIl(bottom, SanctuaryBottomHash);
		InstallSanctuaryBottomCaller(provider, hooks, "PrepareUnderworldArea",
			typeof(void), 1, typeof(int), typeof(int), typeof(int), typeof(int),
			typeof(int), typeof(int));
		InstallSanctuaryBottomCaller(provider, hooks, "SetRect",
			typeof(Rectangle), 1);
		InstallSanctuaryBottomCaller(provider, hooks, "GenerateSanctuary",
			typeof(void), 22);
		InstallSanctuaryBottomCaller(provider, hooks, "GeneratePillar",
			typeof(void), 1, typeof(int), typeof(int));
		InstallSanctuaryBottomCaller(provider, hooks, "GenerateBottomCorridor",
			typeof(void), 1);
		InstallSanctuaryBottomCaller(provider, hooks, "TryErodingBlocks",
			typeof(void), 1, typeof(int), typeof(int));
	}

	private static void InstallSanctuaryBottomCaller(Mod provider,
		List<IDisposable> hooks, string name, Type result, int expectedCalls,
		params Type[] parameters)
	{
		var caller = Exact66ProviderGate.Method(provider, SanctuaryType, name,
			true, result, parameters);
		hooks.Add(new ILHook(caller, il =>
			PatchSanctuaryBottomCalls(il, name, expectedCalls)));
	}

	private static void PatchSanctuaryBottomCalls(ILContext il, string caller,
		int expectedCalls)
	{
		ILCursor cursor = new(il);
		int replaced = 0;
		while (cursor.TryGotoNext(MoveType.Before, IsSanctuaryBottomCall))
		{
			cursor.Remove();
			cursor.EmitDelegate<Func<int>>(GetBoundSanctuaryBottom);
			replaced++;
		}
		if (replaced != expectedCalls)
			throw new InvalidOperationException($"SOTS Sanctuary {caller} has " +
				$"{replaced} Bottom calls; expected {expectedCalls}.");
	}

	private static bool IsSanctuaryBottomCall(Instruction instruction) =>
		instruction.OpCode == OpCodes.Call &&
		instruction.Operand is MethodReference called &&
		called.DeclaringType.FullName == SanctuaryType &&
		called.Name == "get_Bottom";

	private static int GetBoundSanctuaryBottom() =>
		BoundSanctuaryBottom(Main.maxTilesY - 1);

	private static int BoundSanctuaryBottom(int original)
	{
		if (!ContinentalAdapterMath.Active()) return original;
		int bounded = Math.Min(NativeSanctuaryPlacementSystem.BoundBottom(original), Main.maxTilesY - 3);
		SotsWorldgenReceipt.MarkSanctuaryBottom(bounded);
		return bounded;
	}
}
