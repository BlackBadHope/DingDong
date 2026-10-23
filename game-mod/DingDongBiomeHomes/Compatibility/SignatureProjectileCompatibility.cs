using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;
internal static class SignatureProjectileCompatibility
{
    private static GlobalItem template;
    private static Type itemStateType;
    private static Func<object, Item> sourceItem;
    private static Action<object, GlobalItem> bindItemState;
    private static Action<object, object> bindItemConfig, bindProjectileConfig;
    private static SignatureProjectileDamageReader reader;

    internal static void Install(List<ILHook> hooks)
    {
        Type projectile = R19CompatibilitySystem.Provider("SignatureEquipment", "1.9.1", "SignatureEquipment.SignatureGlobalProjectile");
        if (projectile == null) return;
        var mod = ModLoader.GetMod("SignatureEquipment");
        itemStateType = mod.Code.GetType("SignatureEquipment.SignatureGlobalItem", true);
        Type config = mod.Code.GetType("SignatureEquipment.Configs.SignatureEquipmentServerConfig", true);
        template = mod.GetContent<GlobalItem>().Single(g => g.GetType() == itemStateType);
        reader = new SignatureProjectileDamageReader(itemStateType, config);
        sourceItem = SignatureProjectileDamageReader.Read<Item>(projectile, "ProjectileCreatorItem");
        bindItemState = SignatureProjectileDamageReader.Write<GlobalItem>(projectile, "SignatureItem");
        bindItemConfig = SignatureProjectileDamageReader.Write<object>(itemStateType, "config");
        bindProjectileConfig = SignatureProjectileDamageReader.Write<object>(projectile, "config");
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "OnSpawn"), PatchSpawn));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "ModifyHitNPC"), PatchHit));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "OnSpawnByParentProjectile"), PatchBoundSource));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "OnSpawnCloneProjectile"), PatchBoundSource));
    }

    // Use the provider's actual registered template. No ResolveReflection-based
    // generic return may be stored into a field owned by another load context.
    private static GlobalItem FindGlobal(Item item)
    {
        if (item == null || item.IsAir || template == null || !item.TryGetGlobalItem(template, out GlobalItem state) || state == null || state.GetType() != itemStateType) return null;
        return state;
    }
    private static bool PrepareHit(object projectileState)
    {
        Item item = sourceItem(projectileState);
        GlobalItem state = FindGlobal(item);
        if (state == null) return false;
        object config = reader.Config();
        if (config == null) return false;
        // Refresh both old config caches and the per-projectile item-state cache.
        // Live configuration changes and legitimate Signature levels still apply.
        bindItemConfig(state, config); bindProjectileConfig(projectileState, config);
        bindItemState(projectileState, state);
        return true;
    }
    private static int Damage(Item item, bool capped)
    {
        GlobalItem state = FindGlobal(item);
        return state == null ? 0 : reader.Damage(state, capped, SignatureBalanceSystem.Available?SignatureBalanceSystem.Stage:0);
    }
    private static bool HasOwner(Projectile projectile, IEntitySource source) => projectile != null && source != null && projectile.owner >= 0 && projectile.owner < Main.maxPlayers && Main.player[projectile.owner] != null;

    private static void PatchSpawn(ILContext il)
    {
        var cursor = new ILCursor(il); var first = cursor.Next;
        cursor.Emit(OpCodes.Ldarg_1); cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate<Func<Projectile, IEntitySource, bool>>(HasOwner);
        cursor.Emit(OpCodes.Brtrue, first); cursor.Emit(OpCodes.Ret);
        int replaced = 0;
        while (cursor.TryGotoNext(MoveType.AfterLabel, IsSignatureLookup))
        {
            var call = (GenericInstanceMethod)cursor.Next.Operand;
            if (call.Parameters.Count != 0) throw new InvalidOperationException("Unexpected Signature item-lookup arguments.");
            TypeReference resultType = call.GenericArguments[0];
            // Keep branches targeting this lookup on the replacement, before
            // removing the old call. Removing first retargets a non-null HeldItem
            // branch straight to stfld and stores Item as SignatureGlobalItem.
            cursor.EmitDelegate<Func<Item, GlobalItem>>(FindGlobal);
            cursor.Emit(OpCodes.Castclass, resultType);
            cursor.Remove();
            replaced++;
        }
        if (replaced != 4) throw new InvalidOperationException($"Signature 1.9.1 spawn contract changed: {replaced} lookups.");
    }
    private static bool IsSignatureLookup(Instruction i) => i.Operand is GenericInstanceMethod call && call.Name == "GetGlobalItem" && call.DeclaringType.FullName == "Terraria.Item" && call.GenericArguments.Count == 1 && call.GenericArguments[0].FullName == "SignatureEquipment.SignatureGlobalItem";

    private static void PatchHit(ILContext il)
    {
        var itemField = (FieldReference)il.Body.Instructions.First(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f && f.Name == "ProjectileCreatorItem").Operand;
        var calls = il.Body.Instructions.Where(i => i.Operand is MethodReference m && m.DeclaringType.FullName == "SignatureEquipment.SignatureGlobalItem" && m.Name is "get_DamageCapped" or "get_DamageNotCapped").ToArray();
        if (calls.Length != 2) throw new InvalidOperationException("Signature 1.9.1 hit contract changed.");
        foreach (var instruction in calls)
        {
            if (instruction.Previous.OpCode != OpCodes.Ldfld || instruction.Previous.Operand is not FieldReference f || f.Name != "SignatureItem") throw new InvalidOperationException("Signature damage receiver shape changed.");
            bool capped = ((MethodReference)instruction.Operand).Name == "get_DamageCapped";
            instruction.Previous.Operand = itemField;
            var at = new ILCursor(il); at.Goto(instruction, MoveType.AfterLabel);
            at.EmitDelegate<Func<Item, int>>(item => Damage(item, capped));
            at.Remove();
        }
        var cursor = new ILCursor(il); var first = cursor.Next;
        cursor.Emit(OpCodes.Ldarg_0); cursor.EmitDelegate<Func<object, bool>>(PrepareHit);
        cursor.Emit(OpCodes.Brtrue, first); cursor.Emit(OpCodes.Ret);
    }
    private static void PatchBoundSource(ILContext il)
    {
        var cursor = new ILCursor(il); var first = cursor.Next;
        cursor.Emit(OpCodes.Ldarg_0); cursor.EmitDelegate<Func<object, bool>>(PrepareHit);
        cursor.Emit(OpCodes.Brtrue, first); cursor.Emit(OpCodes.Ret);
    }
    internal static void Reset()
    {
        template = null; itemStateType = null; sourceItem = null; bindItemState = null;
        bindItemConfig = bindProjectileConfig = null; reader = null;
    }
}
