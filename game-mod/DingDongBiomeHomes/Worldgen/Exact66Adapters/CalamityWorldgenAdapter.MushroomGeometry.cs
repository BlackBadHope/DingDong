using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class CalamityWorldgenAdapter
{
	private static void PatchMushroomGeometry(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => i.MatchLdsfld(typeof(Main), nameof(Main.maxTilesX)),
			i => i.OpCode == OpCodes.Conv_R8,
			i => i.MatchLdcR8(4200.0), i => i.OpCode == OpCodes.Div))
			throw new InvalidOperationException("Native ShroomPatch geometry scale changed.");
		cursor.EmitDelegate<Func<double, double>>(original =>
			ContinentalAdapterMath.Active() && WorldCompilerRuntimeState.Current == null
				? NativeMushroomScale.Geometry(original, Main.maxTilesX, Main.maxTilesY)
				: original);
	}
}
