using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastalPatches
{
	internal static void GraveyardRange(ILContext il)
	{
		// RuntimeDetour renames the method to a DMD wrapper. Match the ABI, not
		// the offline Cecil name; the original member is resolved by exact name.
		if (!il.Method.IsStatic || il.Method.ReturnType.FullName != "System.Boolean" ||
			il.Method.Parameters.Count != 9 || il.Method.Parameters.Take(5).Any(p => p.ParameterType.FullName != "System.Int32") ||
			il.Method.Parameters[5].ParameterType.FullName != "System.Int32[]&" ||
			il.Method.Parameters.Skip(6).Any(p => p.ParameterType.FullName != "System.Int32&"))
			throw new InvalidOperationException("Thorium graveyard scan signature changed: " + il.Method.FullName);
		var c = new ILCursor(il);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate<Func<int, int>>(NativeCoastalSystem.ScanStart);
		c.Emit(OpCodes.Starg, il.Method.Parameters[0]);
		c.Emit(OpCodes.Ldarg_1);
		c.EmitDelegate<Func<int, int>>(NativeCoastalSystem.ScanEnd);
		c.Emit(OpCodes.Starg, il.Method.Parameters[1]);
	}

	internal static void AirClearance(ILContext il, int native)
	{
		var sites = il.Body.Instructions.Where(i => i.MatchLdcI4(native) && i.Next?.OpCode == OpCodes.Sub).ToArray();
		if (sites.Length != 1) throw new InvalidOperationException("Calamity coastal clearance kernel changed: " + il.Method.Name);
		var c = new ILCursor(il);
		c.Goto(sites[0], MoveType.After);
		c.EmitDelegate<Func<int, int>>(NativeCoastalSystem.ScaleClearance);
	}
}
