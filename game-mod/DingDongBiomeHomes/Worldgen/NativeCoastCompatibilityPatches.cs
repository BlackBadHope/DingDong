using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastCompatibilityPatches
{
	internal static bool OceanCall(Instruction i)=>i.OpCode==OpCodes.Call&&i.Operand is MethodReference m&&m.DeclaringType?.FullName=="Terraria.WorldGen"&&m.Name=="oceanDepths";
	internal static bool CoastField(Instruction i)=>i.OpCode==OpCodes.Ldsfld&&i.Operand is FieldReference f&&f.DeclaringType.FullName=="Terraria.WorldGen"&&f.Name is "beachDistance" or "oceanDistance";
	internal static bool RightEdge(Instruction field)=>field.Previous?.MatchLdsfld("Terraria.Main","maxTilesX")==true;
	internal static void Apply(ILContext il,CoastTarget target,Func<int,int,bool> ocean,Func<int,bool,int> distance)
	{
		var calls=il.Body.Instructions.Where(OceanCall).ToArray();
		var fields=il.Body.Instructions.Where(CoastField).ToArray();
		if(calls.Length!=target.OceanCalls||fields.Count(RightEdge)!=target.Right||fields.Count(f=>!RightEdge(f))!=target.Left)
			throw new InvalidOperationException("Coast call-site shape changed: "+il.Method.FullName);
		foreach(var call in calls){call.OpCode=OpCodes.Nop;call.Operand=null;var c=new ILCursor(il);c.Goto(call,MoveType.After);c.EmitDelegate(ocean);}
		foreach(var field in fields){bool right=RightEdge(field);var c=new ILCursor(il);c.Goto(field,MoveType.After);c.EmitDelegate<Func<int,int>>(native=>distance(native,right));}
	}
}
