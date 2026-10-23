using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// Independent vanilla kernel optimization; provider placement hooks are not replaced.
internal sealed partial class NativeDesertSpatialSystem : ModSystem
{
	private ILHook hook;
	private ILHook geometryHook;

	public override void Load()
	{
		Type hive = typeof(WorldGen).Assembly.GetType(
			"Terraria.GameContent.Biomes.Desert.DesertHive", throwOnError: true);
		MethodInfo method = hive.GetMethod("PlaceClustersArea", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new MissingMethodException(hive.FullName, "PlaceClustersArea");
		hook = new ILHook(method, Patch);
		Type description = typeof(WorldGen).Assembly.GetType("Terraria.GameContent.Biomes.Desert.DesertDescription", true);
		geometryHook = new ILHook(description.GetMethod("CreateFromPlacement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), PatchLocalGeometry);
		InstallSunkenSea();
		Mod.Logger.Info("DINGDONG_DESERT_SPATIAL|installed=True|predicate=vanilla|order=original|rng=unchanged");
	}

	public override void Unload()
	{
		hook?.Dispose(); hook = null;
		geometryHook?.Dispose(); geometryHook = null;
		sunkenSeaHook?.Dispose(); sunkenSeaHook = null;
	}

	private static void PatchLocalGeometry(ILContext il)
	{
		var c=new ILCursor(il);
		if(!c.TryGotoNext(MoveType.After, i=>i.MatchLdsfld("Terraria.Main","maxTilesX"),
			i=>i.OpCode==OpCodes.Conv_R8, i=>i.MatchLdcR8(4200d), i=>i.OpCode==OpCodes.Div))
			throw new InvalidOperationException("Desert local geometry scale changed.");
		c.EmitDelegate<Func<double,double>>(scale=>ContinentalWorldDimensions.IsContinental ? Math.Min(scale,2d) : scale);
	}

	private static void Patch(ILContext il)
	{
		AssertLocalExclusion(il, false);
		ILCursor condition = new(il);
		if (!condition.TryGotoNext(MoveType.AfterLabel,
			i => i.MatchLdloc(21), i => i.MatchLdarg(1),
			i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference m && m.Name == "get_Count",
			i => i.OpCode == OpCodes.Blt))
			throw new InvalidOperationException("Desert cluster loop condition changed.");
		if (il.Body.Variables[14].VariableType.FullName != typeof(Vector2D).FullName ||
			il.Body.Variables[21].VariableType.MetadataType != MetadataType.Int32)
			throw new InvalidOperationException("Desert cluster loop locals changed.");
		VariableDefinition index = new(il.Import(typeof(DesertClusterSpatialIndex)));
		il.Body.Variables.Add(index);
		Instruction original = condition.Next;
		condition.Emit(OpCodes.Ldloc, index);
		condition.Emit(OpCodes.Brfalse, original);
		condition.Emit(OpCodes.Ldloc, index);
		condition.Emit(OpCodes.Ldloc, il.Body.Variables[14]);
		condition.Emit(OpCodes.Ldloc, il.Body.Variables[21]);
		condition.EmitDelegate<Func<DesertClusterSpatialIndex, Vector2D, int, int>>(
			static (map, point, first) => map?.Next(point.X, point.Y, first) ?? first);
		condition.Emit(OpCodes.Stloc, il.Body.Variables[21]);
		ILCursor start = new(il);
		start.Emit(OpCodes.Ldarg_1);
		start.EmitDelegate<Func<object, DesertClusterSpatialIndex>>(DesertClusterIndexSnapshot.Create);
		start.Emit(OpCodes.Stloc, index);
	}
}
