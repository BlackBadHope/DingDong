using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativePoiDensityPatches
{
	internal static void Jungle(ILContext il)
	{
		ILCursor cursor = new(il);
		if (!cursor.TryGotoNext(MoveType.After,
			i => i.MatchLdloc(2), i => Field(i, "Terraria.Main", "maxTilesX"),
			i => i.OpCode == OpCodes.Conv_R8, i => i.MatchLdcR8(4200d),
			i => i.OpCode == OpCodes.Div, i => i.OpCode == OpCodes.Mul) ||
			!cursor.Next.MatchStloc(2))
			throw new InvalidOperationException("POI jungle target kernel changed.");
		cursor.EmitDelegate<Func<double, double>>(NativePoiDensitySystem.JungleTarget);
	}

	internal static void Sky(ILContext il)
	{
		Instruction[] reads = il.Body.Instructions.Where(i =>
			Field(i, "Terraria.WorldBuilding.GenVars", "skyLakes")).ToArray();
		if (reads.Length != 1 || reads[0].Next.OpCode != OpCodes.Add)
			throw new InvalidOperationException("POI sky-lake count kernel changed.");
		ILCursor cursor = new(il);
		cursor.Goto(reads[0], MoveType.After);
		cursor.EmitDelegate<Func<int, int>>(NativePoiDensitySystem.SkyTarget);
	}

	internal static void Range(ILContext il, string key, string kind)
	{
		Instruction[] keys = il.Body.Instructions.Where(i => i.MatchLdstr(key)).ToArray();
		if (keys.Length != 1)
			throw new InvalidOperationException("POI range key changed: " + key);
		Instruction get = keys[0].Next;
		Instruction rng = get?.Next;
		Instruction draw = rng?.Next;
		if (!Call(get, "Terraria.IO.GameConfiguration", "Get") ||
			get.Operand is not GenericInstanceMethod generic ||
			generic.GenericArguments.Count != 1 ||
			generic.GenericArguments[0].FullName != typeof(WorldGenRange).FullName ||
			!Call(rng, "Terraria.WorldGen", "get_genRand") ||
			!Call(draw, typeof(WorldGenRange).FullName, "GetRandom"))
			throw new InvalidOperationException("POI range draw kernel changed: " + key);
		ILCursor cursor = new(il);
		cursor.Goto(draw, MoveType.Before);
		cursor.Remove();
		cursor.EmitDelegate<Func<WorldGenRange, UnifiedRandom, int>>(
			(range, random) => NativePoiDensitySystem.RangeTarget(range, random, kind));
	}

	private static bool Field(Instruction i, string type, string name) =>
		i.OpCode == OpCodes.Ldsfld && i.Operand is FieldReference f &&
		f.DeclaringType.FullName == type && f.Name == name;

	private static bool Call(Instruction i, string type, string name) =>
		i != null && (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
		i.Operand is MethodReference m && m.DeclaringType.FullName == type && m.Name == name;
}
