using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private delegate void SelectGeode(ref int x, ref int y, int direction);

	private static void InstallBigGeodeProtection(Mod provider, List<IDisposable> hooks)
	{
		var method = Exact66ProviderGate.Method(provider, "SOTS.WorldgenHelpers.SOTSWorldgenHelper",
			"GenerateBigGeode", true, typeof(void), typeof(int), typeof(int), typeof(int));
		Exact66ProviderGate.AssertIl(method, "75F8075B88DDC84BAE5F9FF6AF1537A58418107B4E3A2C7E623CEC321B4742C0");
		hooks.Add(new ILHook(method, il =>
		{
			ILCursor cursor = new(il);
			cursor.Emit(OpCodes.Ldarga_S, il.Method.Parameters[0]);
			cursor.Emit(OpCodes.Ldarga_S, il.Method.Parameters[1]);
			cursor.Emit(OpCodes.Ldarg_2);
			cursor.EmitDelegate<SelectGeode>(SelectBigGeode);
		}));
	}

	private static void SelectBigGeode(ref int x, ref int y, int direction)
	{
		if (!CompilerVillageActive() || direction is not (-1 or 1)) return;
		var plan = WorldCompilerRuntimeState.Current;
		(int cx, int cy) = SotsBigGeodePlacement.Find(x, y, direction,
			plan.Width, plan.Height, villageProtected.Overlaps);
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_SOTS_BIG_GEODE|verdict=ADMITTED|original={x},{y}|selected={cx},{cy}|nativeGenerator=True");
		x = cx; y = cy;
		villageProtected.ReserveExisting(SotsBigGeodePlacement.Footprint(x, y, direction));
	}
}
