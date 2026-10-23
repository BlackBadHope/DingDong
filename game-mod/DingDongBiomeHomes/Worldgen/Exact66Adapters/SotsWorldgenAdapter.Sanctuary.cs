using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private const string SanctuaryType =
		"SOTS.WorldgenHelpers.SanctuaryWorldgenHelper";
	private const string SanctuaryKillLineHash =
		"2DAD5076BE725679CDFE7E676F8CA969C5585C202263411A890A7B026B3D13DB";

	private static void InstallSanctuaryBounds(Mod provider, List<IDisposable> hooks)
	{
		InstallSanctuaryBottomCallSites(provider, hooks);
		var killLine = Exact66ProviderGate.Method(provider, SanctuaryType,
			"KillLine", true, typeof(void), typeof(int), typeof(int), typeof(int), typeof(int));
		Exact66ProviderGate.AssertIl(killLine, SanctuaryKillLineHash);
		hooks.Add(new ILHook(killLine, PatchSanctuaryKillLine));
	}

	private static void PatchSanctuaryKillLine(ILContext il)
	{
		ILCursor entry = new(il);
		Instruction original = entry.Next ??
			throw new InvalidOperationException("SOTS Sanctuary KillLine is empty.");
		EmitSanctuaryGuard(entry, original, false);

		ILCursor moved = new(il);
		Expect(moved.TryGotoNext(MoveType.After,
			i => i.MatchLdarg(1), i => i.MatchLdarg(3),
			i => i.OpCode == OpCodes.Add, i => IsArgumentStore(i, 1)),
			"SOTS Sanctuary moved coordinate");
		Instruction neighbors = moved.Next ??
			throw new InvalidOperationException("SOTS Sanctuary neighbor scan is missing.");
		EmitSanctuaryGuard(moved, neighbors, true);
	}

	private static void EmitSanctuaryGuard(ILCursor cursor, Instruction admitted,
		bool includeNeighbors)
	{
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldarg_1);
		if (includeNeighbors)
		{
			cursor.Emit(OpCodes.Ldarg_2);
			cursor.Emit(OpCodes.Ldarg_3);
			cursor.EmitDelegate<Func<int, int, int, int, bool>>(AdmitSanctuaryNeighborhood);
		}
		else cursor.EmitDelegate<Func<int, int, bool>>(AdmitSanctuaryTile);
		cursor.Emit(OpCodes.Brtrue, admitted);
		cursor.Emit(OpCodes.Ret);
	}

	private static bool AdmitSanctuaryTile(int x, int y) => MarkSanctuaryAdmission(
		!ContinentalAdapterMath.Active() || InSanctuaryWorld(x, y));

	private static bool AdmitSanctuaryNeighborhood(int x, int y, int dirX, int dirY) =>
		MarkSanctuaryAdmission(!ContinentalAdapterMath.Active() ||
			InSanctuaryWorld(x, y) && InSanctuaryWorld(x + dirY, y + dirX) &&
			InSanctuaryWorld(x - dirY, y - dirX));

	private static bool MarkSanctuaryAdmission(bool admitted)
	{
		if (ContinentalAdapterMath.Active()) SotsWorldgenReceipt.MarkSanctuaryGuard(admitted);
		return admitted;
	}

	private static bool InSanctuaryWorld(int x, int y) =>
		x >= 1 && y >= 1 && x < Main.maxTilesX - 1 && y < Main.maxTilesY - 1;

	private static bool IsArgumentStore(Instruction instruction, int index) =>
		instruction.OpCode is var op && (op == OpCodes.Starg || op == OpCodes.Starg_S) &&
		instruction.Operand is ParameterDefinition parameter && parameter.Index == index;
}
