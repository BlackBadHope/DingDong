using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Compatibility;

public sealed class ThoriumInformationalIconPlacement : ModSystem
{
    internal const int ShiftLeft = 80;
    internal const int ShiftDown = 18;
    private ILHook hook;
    internal static bool Installed {get;private set;}
    public override void Load()
    {
        if(Main.dedServ||!ModLoader.TryGetMod("ThoriumMod",out var mod))return;
        var type=mod.Code.GetType("ThoriumMod.UI.ResourceBars.InformationalIcons",true);
        var method=type.GetMethod("DisplayIconNearEquips",BindingFlags.Static|BindingFlags.NonPublic);
        if(method==null||method.GetParameters().Length!=8||method.GetParameters()[2].ParameterType!=typeof(int))
            throw new InvalidOperationException("Thorium informational icon layout contract changed");
        hook=new ILHook(method,il=>
        {
            // Move the original anchor before the provider clamps and draws it.
            // Text and mouse hit testing continue to use that same anchor.
            var width=il.Body.Instructions.Where(i=>i.MatchLdsfld("Terraria.Main","screenWidth")||i.MatchCall("Terraria.Main","get_screenWidth")).ToArray();
            if(width.Length!=2)throw new InvalidOperationException("Thorium informational icon horizontal anchors changed");
            var c=new ILCursor(il);c.Emit(OpCodes.Ldarg_2);c.Emit(OpCodes.Ldc_I4,ShiftDown);c.Emit(OpCodes.Add);c.Emit(OpCodes.Starg,il.Method.Parameters[2]);
            c.Goto(width[0],MoveType.After);c.Emit(OpCodes.Ldc_I4,ShiftLeft);c.Emit(OpCodes.Sub);
        });
        Installed=true;
    }
    public override void Unload(){hook?.Dispose();hook=null;Installed=false;}
}
