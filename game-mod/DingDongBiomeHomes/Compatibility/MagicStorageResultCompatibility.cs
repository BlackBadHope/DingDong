using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;

namespace DingDongBiomeHomes.Compatibility;

internal static class MagicStorageResultCompatibility
{
	internal static void Install(List<ILHook> hooks)
	{
		Type type = R19CompatibilitySystem.Provider("MagicStorage", "0.7.0.11", "MagicStorage.NetHelper");
		if (type != null) hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "ReciveServerStorageResult"), PatchResult));
	}

	// TEStorageHeart.ProcessClientOperations writes the original requested stack
	// after ItemIO.Send for operations 6 and 7. Both receiver routes omit it.
	// Consume that defined field only; preserve the native returned item exactly.
	private static Item ReadRequestedStack(Item item, BinaryReader reader, int operation)
	{
		if (operation is 6 or 7) reader.ReadInt32();
		return item;
	}

	private static void PatchResult(ILContext il)
	{
		if (il.Body.Variables.Count == 0 || il.Body.Variables[0].VariableType.FullName != "MagicStorage.Components.TEStorageHeart/Operation")
			throw new InvalidOperationException("MagicStorage result operation contract changed.");
		var cursor = new ILCursor(il);
		int replaced = 0;
		while (cursor.TryGotoNext(MoveType.After, i => i.Operand is MethodReference m &&
			m.DeclaringType.FullName == "Terraria.ModLoader.IO.ItemIO" && m.Name == "Receive"))
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.EmitDelegate<Func<Item, BinaryReader, int, Item>>(ReadRequestedStack);
			replaced++;
		}
		if (replaced != 6) throw new InvalidOperationException($"MagicStorage result contract changed: {replaced} item reads.");
	}
}
