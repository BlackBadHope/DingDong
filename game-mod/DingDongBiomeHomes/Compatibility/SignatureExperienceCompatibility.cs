using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

internal static class SignatureExperienceCompatibility
{
    private sealed class Ledger { internal ulong Last; internal bool Started; internal double Fraction; }
    private static ConditionalWeakTable<Item, Dictionary<int, Ledger>> budgets = new();
    internal static void Reset() => budgets = new();
    internal static void Install(List<ILHook> hooks, Mod provider)
    {
        Type player=provider.Code.GetType("SignatureEquipment.SignatureEquipmentPlayer",true);
        Type npc=provider.Code.GetType("SignatureEquipment.SignatureGlobalNPC",true);
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(player,"OnHit"), il => {
            var sites=il.Body.Instructions.Where(IsAddExp).ToArray();
            if(sites.Length!=1) throw new InvalidOperationException("Signature hit XP contract changed.");
            var c=new ILCursor(il);c.Goto(sites[0],MoveType.AfterLabel);
            c.Emit(OpCodes.Pop); // replace the damage-based reward, keep native state receiver
            // RuntimeDetour's static clone includes the original this argument
            // in Parameters. Use the verified IL argument slots directly.
            c.Emit(OpCodes.Ldarg_2);c.Emit(OpCodes.Ldarg_0);c.Emit(OpCodes.Ldarg_3);c.Emit(OpCodes.Ldarg,4);
            c.EmitDelegate<Func<Item,ModPlayer,int,NPC,int>>(Award);
        }));
        // A separate per-kill multiplier would bypass the shared time budget.
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(npc,"CheckDead"), il => {
            var sites=il.Body.Instructions.Where(IsAddExp).ToArray();
            if(sites.Length!=1) throw new InvalidOperationException("Signature kill XP contract changed.");
            var c=new ILCursor(il);c.Goto(sites[0],MoveType.AfterLabel);c.Emit(OpCodes.Pop);c.Emit(OpCodes.Ldc_I4_0);
        }));
    }
    private static bool IsAddExp(Instruction i) => i.Operand is MethodReference m &&
        m.DeclaringType.FullName=="SignatureEquipment.SignatureGlobalItem" && m.Name=="AddExp";
    private static int Award(Item item,ModPlayer source,int damage,NPC target)
    {
        if(item==null || item.IsAir || item.damage<=0 || damage<=0 || target==null || target.friendly ||
            target.type==NPCID.TargetDummy || target.SpawnedFromStatue || target.lifeMax<=5) return 0;
        Player player=source.Player;
        if(player==null || !player.active || player.dead || (Main.netMode==NetmodeID.MultiplayerClient && player.whoAmI!=Main.myPlayer))return 0;
        Dictionary<int,Ledger> byOwner=budgets.GetValue(item,_=>new());
        if(!byOwner.TryGetValue(player.whoAmI,out var ledger))byOwner[player.whoAmI]=ledger=new();
        ulong now=Main.GameUpdateCount;
        ulong elapsed=ledger.Started && now>=ledger.Last ? Math.Min(now-ledger.Last,60UL) : 10UL;
        if(elapsed<10)return 0;
        ledger.Started=true;ledger.Last=now;
        ledger.Fraction+=SignatureExperienceRules.PerSecond(SignatureBalanceSystem.RawLevel(item),SignatureBalanceSystem.Stage,
            item.DamageType.CountsAsClass(DamageClass.Melee))*elapsed/60d;
        int award=(int)Math.Floor(ledger.Fraction);ledger.Fraction-=award;
        return award;
    }
}
