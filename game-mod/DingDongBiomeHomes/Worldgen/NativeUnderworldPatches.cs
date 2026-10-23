using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeUnderworldPatches
{
	internal static void Terrain(ILContext il)
	{
		// Match the two depth draws, not arbitrary Next arguments or feature sizes.
		RandomDepth(il, 150, 190, 160);
		RandomDepth(il, 40, 70, 60);
		BottomOffsets(il, 190);
	}

	internal static void BottomOffsets(ILContext il, int required)
	{
		var offsets = il.Body.Instructions.Where(i => Height(i.Previous) &&
			i.MatchLdcI4(out int value) && value > 20 &&
			i.Next?.OpCode == OpCodes.Sub).ToArray();
		string actual = string.Join(",", offsets.Select(i => { i.MatchLdcI4(out int v); return v; })
			.GroupBy(v => v).OrderBy(g => g.Key).Select(g => $"{g.Key}:{g.Count()}"));
		string expected = required switch {
			190 => "50:2,60:2,65:2,100:3,120:1,135:2,140:2,144:3,145:3,160:3,180:2,190:2,200:3,300:3",
			300 => "40:1,250:5,300:1",
			200 => "200:1",
			_ => throw new ArgumentOutOfRangeException(nameof(required))
		};
		if (actual != expected)
			throw new InvalidOperationException("Underworld depth kernel changed: " + il.Method.Name);
		foreach (var offset in offsets)
		{
			ILCursor cursor = new(il);
			cursor.Goto(offset, MoveType.After);
			offset.MatchLdcI4(out int value);
			// These are the clamp pairs of the two random walks, and a pair of
			// adjacent lava cells (145/144). Translate each pair together: scaling
			// both values stretches relief or turns adjacent cells into a gap.
			int baseline = il.Method.Name == "<AddGenPasses>b__288_27" ? value switch {
				190 or 160 or 145 or 144 => 160,
				60 or 120 => 60,
				100 when offset.Next?.Next?.OpCode.FlowControl == FlowControl.Cond_Branch => 60,
				_ => 0
			} : 0;
			if (baseline == 0) cursor.EmitDelegate<Func<int, int>>(NativeUnderworldSystem.ScaleOffset);
			else cursor.EmitDelegate<Func<int, int>>(native => NativeUnderworldSystem.ScaleDepthDraw(native, baseline));
		}
	}

	internal static void LayerCalls(ILContext il, int expected)
		=> ReplaceLayerCalls(il, expected, _ => false);

	internal static void PatchLayerConsumer(ILContext il, string type, string method, int expected)
	{
		if (type == "Terraria.Liquid" && method == "Update")
		{
			ReplaceLayerCalls(il, expected, _ => false, liquid: true);
			return;
		}
		if (type == "Terraria.NPC" && method == "SpawnWOF")
		{
			SpawnWallOfFlesh(il, expected);
			return;
		}
		if (type == "Terraria.NPC" && method == "VanillaAI_Inner")
		{
			// Calls 2 and 3 (zero based 1 and 2) define the Wall of Flesh
			// draw/combat corridor. The other three belong to unrelated AIs.
			ReplaceLayerCalls(il, expected, index => index is 1 or 2);
			return;
		}
		if (type == "Terraria.Player" && method == "WOFTongue")
		{
			ReplaceLayerCalls(il, expected, _ => true);
			return;
		}
		LayerCalls(il, expected);
	}

	private static void SpawnWallOfFlesh(ILContext il, int expected)
	{
		ReplaceLayerCalls(il, expected, _ => true);
		var legacyGates = il.Body.Instructions.Where(i => Height(i) &&
			i.Next?.MatchLdcI4(205) == true && i.Next.Next?.OpCode == OpCodes.Sub).ToArray();
		if (legacyGates.Length != 1)
			throw new InvalidOperationException("Wall of Flesh summon gate changed.");
		legacyGates[0].OpCode = OpCodes.Nop;
		legacyGates[0].Operand = null;
		legacyGates[0].Next.OpCode = OpCodes.Nop;
		legacyGates[0].Next.Operand = null;
		legacyGates[0].Next.Next.OpCode = OpCodes.Nop;
		legacyGates[0].Next.Next.Operand = null;
		var cursor = new ILCursor(il);
		cursor.Goto(legacyGates[0].Next.Next, MoveType.After);
		cursor.EmitDelegate<Func<int>>(() => NativeUnderworldSystem.Active
			? NativeUnderworldSystem.Layer : Terraria.Main.maxTilesY - 205);
	}

	private static void ReplaceLayerCalls(ILContext il, int expected, Func<int, bool> wallOfFleshCall, bool liquid = false)
	{
		var calls = il.Body.Instructions.Where(i => Call(i, "Terraria.Main", "get_UnderworldLayer")).ToArray();
		if (calls.Length != expected)
			throw new InvalidOperationException($"Underworld caller changed: {il.Method.FullName} {calls.Length}/{expected}");
		for (int index = 0; index < calls.Length; index++)
		{
			var call = calls[index];
			// Replace the call in the consumer itself. Hooking the tiny getter alone
			// cannot intercept copies already inlined by the JIT.
			call.OpCode = OpCodes.Nop;
			call.Operand = null;
			var cursor = new ILCursor(il);
			cursor.Goto(call, MoveType.After);
			if (liquid)
				cursor.EmitDelegate<Func<int>>(() => NativeSotsLiquidScopeSystem.Layer);
			else if (wallOfFleshCall(index))
				cursor.EmitDelegate<Func<int>>(() => NativeUnderworldSystem.WallOfFleshLayer);
			else
				cursor.EmitDelegate<Func<int>>(() => NativeUnderworldSystem.Layer);
		}
	}

	private static void RandomDepth(ILContext il, int min, int max, int baseline)
	{
		var starts = il.Body.Instructions.Where(i => Height(i) &&
			Call(i.Next, "Terraria.WorldGen", "get_genRand") &&
			i.Next.Next.MatchLdcI4(min) && i.Next.Next.Next.MatchLdcI4(max) &&
			Call(i.Next.Next.Next.Next, "Terraria.Utilities.UnifiedRandom", "Next") &&
			i.Next.Next.Next.Next.Next.OpCode == OpCodes.Sub).ToArray();
		if (starts.Length != 1) throw new InvalidOperationException("Underworld RNG depth changed.");
		ILCursor cursor = new(il);
		cursor.Goto(starts[0].Next.Next.Next.Next, MoveType.After);
		// Keep the same RNG draw count and preserve local relief amplitude while
		// shifting the whole band into the expanded biome.
		cursor.EmitDelegate<Func<int, int>>(value =>
			NativeUnderworldSystem.ScaleDepthDraw(value, baseline));
	}

	private static bool Height(Instruction i) => i != null &&
		i.MatchLdsfld("Terraria.Main", "maxTilesY");
	private static bool Call(Instruction i, string type, string name) => i != null &&
		(i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
		i.Operand is MethodReference m && m.DeclaringType?.FullName == type && m.Name == name;
}
