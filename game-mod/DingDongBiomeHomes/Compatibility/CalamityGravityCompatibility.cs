using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;

namespace DingDongBiomeHomes.Compatibility;

internal static class CalamityGravityCompatibility
{
    internal static void Install(List<ILHook> hooks)
    {
        Type type=R19CompatibilitySystem.Provider("CalamityMod","2.2.2","CalamityMod.Systems.Mechanic.GravityManipulationChangesSystem");
        if(type==null)return;
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(type,"DelayGravity"),Patch));
    }
    private static void Patch(ILContext il)
    {
        if(il.Method.Parameters.Count!=2 || il.Method.Parameters[1].ParameterType.FullName!="Terraria.Player")
            throw new InvalidOperationException("Calamity gravity input contract changed.");
        var sites=il.Body.Instructions.Where(i=>i.Operand is MethodReference m && m.Name=="get_Count" &&
            m.DeclaringType.FullName.StartsWith("System.Collections.Generic.List`1")).ToArray();
        if(sites.Length!=1 || sites[0].Previous?.Operand is not MethodReference assigned || assigned.Name!="GetAssignedKeysOrEmpty")
            throw new InvalidOperationException("Calamity gravity keybinding guard changed.");
        var up=il.Body.Instructions.Where(i=>i.MatchLdfld("Terraria.Player","controlUp")).ToArray();
        if(up.Length!=1 || up[0].Next.OpCode.FlowControl!=FlowControl.Cond_Branch)
            throw new InvalidOperationException("Calamity vanilla gravity cancellation changed.");
        // Skip only the provider's cancellation of the vanilla Up toggle.
        // Keep its custom hotkey, mount/ability guards, forced gravity and delay.
        var c=new ILCursor(il);c.Goto(up[0],MoveType.After);
        c.Emit(OpCodes.Pop);c.Emit(OpCodes.Ldc_I4_0);
    }
}
