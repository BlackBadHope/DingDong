using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeAbyssHeightPatches
{
    internal static void CachedHeight(ILContext il, Func<int,int> translate)
    {
        // PlaceAbyss caches maxTilesY in local 1. The direct-field census
        // missed its 22 bottom markers, allowing Voidstone to overwrite Hell.
        var stores=il.Body.Instructions.Where(i=>i.MatchStloc(1)).ToArray();
        if(il.Body.Variables.Count<2 || il.Body.Variables[1].VariableType.MetadataType!=MetadataType.Int32 ||
            stores.Length!=1 || !stores[0].Previous.MatchLdsfld("Terraria.Main","maxTilesY"))
            throw new InvalidOperationException("Abyss cached world-height contract changed.");
        var sites=il.Body.Instructions.Where(i=>i.MatchLdloc(1) && i.Next?.Next?.OpCode==OpCodes.Sub &&
            (i.Next.MatchLdcI4(200) || i.Next.MatchLdcI4(250))).Select(i=>i.Next).ToArray();
        if(sites.Count(i=>i.MatchLdcI4(200))!=21 || sites.Count(i=>i.MatchLdcI4(250))!=1)
            throw new InvalidOperationException("Abyss cached bottom-marker census changed.");
        foreach(var site in sites)
        {
            var c=new ILCursor(il);c.Goto(site,MoveType.After);
            c.EmitDelegate(translate);
        }
    }
}
