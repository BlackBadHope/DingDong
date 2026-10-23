using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed partial class NativeDesertSpatialSystem
{
	private ILHook sunkenSeaHook;

	private void InstallSunkenSea()
	{
		if (!ModLoader.TryGetMod("CalamityMod", out Mod provider)) return;
		Type sea = provider.Code.GetType("CalamityMod.World.SunkenSea", throwOnError: true);
		MethodInfo method = sea.GetMethod("PlaceClusters", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new MissingMethodException(sea.FullName, "PlaceClusters");
		sunkenSeaHook = new ILHook(method, PatchSunkenSea);
		Mod.Logger.Info("DINGDONG_SUNKENSEA_SPATIAL|installed=True|arithmetic=original-float|rng=unchanged");
	}

	private static void PatchSunkenSea(ILContext il)
	{
		AssertLocalExclusion(il, true);
		ILCursor condition = new(il);
		if (!condition.TryGotoNext(MoveType.AfterLabel,
			i => i.MatchLdloc(17), i => i.MatchLdarg(0),
			i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference m && m.Name == "get_Count",
			i => i.OpCode == OpCodes.Blt))
			throw new InvalidOperationException("Sunken Sea cluster loop condition changed.");
		if (il.Body.Variables[11].VariableType.FullName != typeof(Vector2).FullName ||
			il.Body.Variables[17].VariableType.MetadataType != MetadataType.Int32)
			throw new InvalidOperationException("Sunken Sea cluster loop locals changed.");
		VariableDefinition index = new(il.Import(typeof(DesertClusterSpatialIndex)));
		il.Body.Variables.Add(index);
		Instruction original = condition.Next;
		condition.Emit(OpCodes.Ldloc, index);
		condition.Emit(OpCodes.Brfalse, original);
		condition.Emit(OpCodes.Ldloc, index);
		condition.Emit(OpCodes.Ldloc, il.Body.Variables[11]);
		condition.Emit(OpCodes.Ldloc, il.Body.Variables[17]);
		condition.EmitDelegate<Func<DesertClusterSpatialIndex, Vector2, int, int>>(
			static (map, point, first) => map.Next(point.X, point.Y, first));
		condition.Emit(OpCodes.Stloc, il.Body.Variables[17]);
		ILCursor start = new(il);
		start.Emit(OpCodes.Ldarg_0);
		start.EmitDelegate<Func<object, DesertClusterSpatialIndex>>(DesertClusterIndexSnapshot.Create);
		start.Emit(OpCodes.Stloc, index);
	}

	private static void AssertLocalExclusion(ILContext il, bool single)
	{
		int matches = 0;
		ILCursor cursor = new(il);
		while (cursor.TryGotoNext(MoveType.After,
			i => i.MatchCall(typeof(Math), nameof(Math.Abs)),
			i => single ? i.MatchLdcR4(10f) : i.MatchLdcR8(10d),
			i => i.OpCode == OpCodes.Bgt || i.OpCode == OpCodes.Bgt_S ||
				i.OpCode == OpCodes.Bgt_Un || i.OpCode == OpCodes.Bgt_Un_S)) matches++;
		if (matches != 2) throw new InvalidOperationException("Desert +/-10 anchor exclusion changed.");
	}
}
