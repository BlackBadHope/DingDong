using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeHeightCompatibilityPatches
{
	internal static bool LayerCall(Instruction i) => i.Operand is MethodReference m &&
		(i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
		m.DeclaringType?.FullName == "Terraria.Main" && m.Name == "get_UnderworldLayer";

	internal static bool BottomLiteral(Instruction start, out Instruction literal, out int value)
	{
		literal = null; value = 0;
		if (!start.MatchLdsfld("Terraria.Main", "maxTilesY")) return false;
		literal = start.Next;
		// SubworldLibrary 2.3.0.1 wraps DrawBackground's offset in its
		// hideUnderworld branch. Keep that branch intact and translate only
		// the ordinary-world arm, rather than disabling shape validation.
		if (literal?.MatchLdsfld("SubworldLibrary.SubworldSystem", "hideUnderworld") == true)
		{
			Instruction conditional = literal.Next, conversion = conditional?.Next,
				jump = conversion?.Next, normalOffset = jump?.Next;
			if (conditional == null || (conditional.OpCode != OpCodes.Brfalse && conditional.OpCode != OpCodes.Brfalse_S) ||
				conversion?.OpCode != OpCodes.Conv_R8 || jump == null ||
				(jump.OpCode != OpCodes.Br && jump.OpCode != OpCodes.Br_S) || normalOffset == null)
				return false;
			Instruction destination = conditional.Operand is ILLabel label ? label.Target : conditional.Operand as Instruction;
			if (destination != normalOffset) return false;
			literal = normalOffset;
		}
		if (literal?.OpCode == OpCodes.Conv_R4 || literal?.OpCode == OpCodes.Conv_R8) literal = literal.Next;
		if (literal?.Next?.OpCode != OpCodes.Sub) return false;
		if (literal.MatchLdcI4(out value)) return true;
		if (literal.OpCode == OpCodes.Ldc_R4 && literal.Operand is float f && f == (int)f) { value = (int)f; return true; }
		if (literal.OpCode == OpCodes.Ldc_R8 && literal.Operand is double d && d == (int)d) { value = (int)d; return true; }
		return false;
	}

	internal static void Apply(ILContext il, HeightTarget target, Func<int> layer, Func<int, int> translate)
	{
		// Check the complete audited shape before editing anything. A foreign
		// delegate without a declaring type is neither a crash nor a match.
		var calls = il.Body.Instructions.Where(LayerCall).ToArray();
		if (calls.Length != target.LayerCalls)
			throw new InvalidOperationException($"Height layer shape changed: {il.Method.FullName} {calls.Length}/{target.LayerCalls}");
		var sites = il.Body.Instructions.Select(i => BottomLiteral(i, out var literal, out int value)
			? (Literal: literal, Value: value) : (Literal: (Instruction)null, Value: 0))
			.Where(p => p.Literal != null && target.Offsets.Any(o => o.Value == p.Value)).ToArray();
		foreach (var offset in target.Offsets)
			if (sites.Count(s => s.Value == offset.Value) != offset.Count)
				throw new InvalidOperationException($"Height bottom shape changed: {il.Method.FullName} offset={offset.Value}");
		if (target.Provider == "CalamityMod" && target.Type == "CalamityMod.World.Abyss" && target.Name == "PlaceAbyss")
			NativeAbyssHeightPatches.CachedHeight(il, translate);

		foreach (Instruction call in calls)
		{
			call.OpCode = OpCodes.Nop; call.Operand = null;
			var cursor = new ILCursor(il); cursor.Goto(call, MoveType.After);
			cursor.EmitDelegate(layer);
		}
		foreach (var site in sites)
		{
			var cursor = new ILCursor(il); cursor.Goto(site.Literal, MoveType.After);
			if (site.Literal.OpCode == OpCodes.Ldc_R4)
				cursor.EmitDelegate<Func<float, float>>(value => translate((int)value));
			else if (site.Literal.OpCode == OpCodes.Ldc_R8)
				cursor.EmitDelegate<Func<double, double>>(value => translate((int)value));
			else cursor.EmitDelegate(translate);
		}
	}
}
