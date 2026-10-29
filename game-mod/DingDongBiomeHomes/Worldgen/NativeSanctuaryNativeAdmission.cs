using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Worldgen;

// Reserve the future Sanctuary before vanilla writes houses or their chests.
// Exact66 admission pins PackWorldgenFix, whose Calamity override is 3*W/8.
internal static class NativeSanctuaryNativeAdmission
{
	private static bool Active => NativeSanctuaryPlacementSystem.FreshGeneration &&
		ModLoader.HasMod("PackWorldgenFix") && ModLoader.HasMod("CalamityMod") && ModLoader.HasMod("SOTS");
	private static int Anchor => Main.maxTilesX * 3 / 8;

	internal static bool PermitChest(int x, int y)
	{
		if (!Active) return true;
		int ceiling = NativeUnderworldSystem.Layer + 80;
		return Math.Abs(x - Anchor) > 520 || y < ceiling - 60 || y > ceiling + 215;
	}

	private static bool PermitHouse(int x) => !Active || Math.Abs(x - Anchor) > 720;
	private static bool PermitFinalChest(int x, int y) =>
		NativeCoastPlacement.CurrentPass == "SOTS: Sanctuary" || PermitChest(x,y);

	internal static void AfterPass(string name)
	{
		if (name != "SOTS: Sanctuary" || !Active) return;
		int ceiling = NativeSanctuaryPlacementSystem.PersistedCeiling;
		if (ceiling <= 0) throw new InvalidOperationException("Sanctuary generated without its absolute placement.");
		// SOTS does not register its native structure in StructureMap. Protect the
		// completed structure before the 61-residence planner admits its own sites.
		GenVars.structures.AddProtectedStructure(new Rectangle(Anchor-510,ceiling-50,1021,258),0);
	}

	internal static void Install(List<ILHook> hooks)
	{
		foreach (var (name, tileType) in new[] { ("HellHouse", typeof(byte)), ("HellFort", typeof(ushort)) })
		{
			MethodInfo method = typeof(WorldGen).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
				null, new[] { typeof(int), typeof(int), tileType, typeof(byte) }, null)
				?? throw new MissingMethodException("Native Sanctuary house admission: " + name);
			hooks.Add(new ILHook(method, PatchHouse));
		}
		MethodInfo chest = typeof(WorldGen).GetMethod(nameof(WorldGen.PlaceChest),
			new[] { typeof(int), typeof(int), typeof(ushort), typeof(bool), typeof(int) })
			?? throw new MissingMethodException("Native Sanctuary final chest admission");
		hooks.Add(new ILHook(chest, PatchChest));
	}

	private static void PatchChest(ILContext il)
	{
		if (!il.Method.IsStatic || il.Method.ReturnType.FullName != "System.Int32" || il.Method.Parameters.Count != 5)
			throw new InvalidOperationException("Native final chest placement ABI changed.");
		var cursor = new ILCursor(il); var original = cursor.Next;
		cursor.Emit(OpCodes.Ldarg_0); cursor.Emit(OpCodes.Ldarg_1);
		cursor.EmitDelegate<Func<int,int,bool>>(PermitFinalChest);
		cursor.Emit(OpCodes.Brtrue, original); cursor.Emit(OpCodes.Ldc_I4_M1); cursor.Emit(OpCodes.Ret);
	}

	private static void PatchHouse(ILContext il)
	{
		if (!il.Method.IsStatic || il.Method.ReturnType.FullName != "System.Void" ||
			il.Method.Parameters.Count != 4 || il.Method.Parameters[0].ParameterType.FullName != "System.Int32")
			throw new InvalidOperationException("Native Hell house admission ABI changed.");
		var cursor = new ILCursor(il);
		var original = cursor.Next;
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<int, bool>>(PermitHouse);
		cursor.Emit(OpCodes.Brtrue, original);
		cursor.Emit(OpCodes.Ret);
	}
}
