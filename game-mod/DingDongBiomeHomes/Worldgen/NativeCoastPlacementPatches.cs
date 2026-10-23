using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastPlacementPatches
{
	internal static void BuriedChest(ILContext il)
	{
		if (!il.Method.IsStatic || il.Method.ReturnType.FullName != "System.Boolean" ||
			il.Method.Parameters.Count != 7 || il.Method.Parameters[0].ParameterType.FullName != "System.Int32" ||
			il.Method.Parameters[1].ParameterType.FullName != "System.Int32")
			throw new InvalidOperationException("Native buried chest ABI changed.");
		var entry = il.Body.Instructions[0]; var c = new ILCursor(il);
		c.Emit(OpCodes.Ldarg_0); c.Emit(OpCodes.Ldarg_1);
		c.EmitDelegate<Func<int, int, bool>>(NativeCoastPlacement.PermitChest);
		c.Emit(OpCodes.Brtrue, entry); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret);
	}
	internal static void CanPlace(ILContext il)
	{
		if (il.Method.ReturnType.FullName != "System.Boolean" ||
			!il.Method.Parameters.Any(p => p.ParameterType.FullName == "Microsoft.Xna.Framework.Rectangle") ||
			il.Method.Parameters.Last().ParameterType.FullName != "System.Int32")
			throw new InvalidOperationException("Native structure placement ABI changed.");
		// Original instance ABI: this, area, allowedTiles, padding. DMD wrappers
		// preserve these argument positions even when making this explicit.
		var entry = il.Body.Instructions[0]; var c = new ILCursor(il);
		c.Emit(OpCodes.Ldarg_1); c.Emit(OpCodes.Ldarg_3);
		c.EmitDelegate<Func<Rectangle, int, bool>>(NativeCoastPlacement.PermitStructure);
		c.Emit(OpCodes.Brtrue, entry); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret);
	}
}
