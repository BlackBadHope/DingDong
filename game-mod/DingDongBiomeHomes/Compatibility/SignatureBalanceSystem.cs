using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

public sealed class SignatureBalanceSystem : ModSystem
{
    private readonly List<ILHook> hooks = new();
    private static GlobalItem template;
    private static Func<object, int> raw, xp, xpNeeded;
    private static Func<object> config;
    private static Func<object, Item> projectileItem;
    private static object appliedConfig;
    private static int appliedStage = -1;
    internal static bool Available => template != null && raw != null;
    internal static bool HasState(Item item) => State(item) != null;
    internal static int Stage => SignatureBalanceRules.StageCap(Main.hardMode, NPC.downedBoss2, NPC.downedPlantBoss, NPC.downedMoonlord);
    internal static int Experience(Item item) => State(item) is object state ? Math.Max(0, xp(state)) : 0;
    internal static int ExperienceNeeded(Item item) => State(item) is object state ? Math.Max(1, xpNeeded(state)) : 500;
    internal static int RawLevel(Item item) => State(item) is object state ? Math.Max(0, raw(state)) : 0;
    private static GlobalItem State(Item item) => item != null && !item.IsAir && template != null &&
        item.TryGetGlobalItem(template, out GlobalItem state) ? state : null;

    public override void PostSetupContent()
    {
        if (!ModLoader.TryGetMod("SignatureEquipment", out Mod provider)) return;
        if (provider.Version != new Version(1, 9, 1))
            throw new InvalidOperationException("Signature balance requires the verified 1.9.1 provider.");
        Type item = provider.Code.GetType("SignatureEquipment.SignatureGlobalItem", true);
        Type projectile = provider.Code.GetType("SignatureEquipment.SignatureGlobalProjectile", true);
        Type settings = provider.Code.GetType("SignatureEquipment.Configs.SignatureEquipmentServerConfig", true);
        template = provider.GetContent<GlobalItem>().Single(g => g.GetType() == item);
        raw = SignatureProjectileDamageReader.Read<int>(item, "signatureLevel");
        xp = SignatureProjectileDamageReader.Read<int>(item, "signatureExpCurrent");
        var stateArg = Expression.Parameter(typeof(object), "state");
        xpNeeded = Expression.Lambda<Func<object, int>>(Expression.Property(Expression.Convert(stateArg, item), "signatureExpToUp"), stateArg).Compile();
        projectileItem = SignatureProjectileDamageReader.Read<Item>(projectile, "ProjectileCreatorItem");
        var instance = typeof(ModContent).GetMethods().Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(settings);
        config = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Call(instance), typeof(object))).Compile();
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(item, "get_MinionSlotsReduceCapped"), PatchDiscount));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(item, "UpdateEquip"), PatchDefence));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "OnSpawn"), PatchSpawn));
        hooks.Add(new ILHook(R19CompatibilitySystem.Method(projectile, "ModifyHitNPC"), PatchHitDamageWrites));
        foreach(string name in new[]{"DamageNotCapped","CritNotCapped","UseTimeNotCapped","UseAnimNotCapped","MeleeSizeNotCapped","ProjectileScaleNotCapped","ProjectileStartVelocityScaleNotCapped","ProjectilePenetrateNotCapped","ProjectileLifeTimeNotCapped","NotUseAmmoChanceNotCapped","ManaCostNotCapped"})
            hooks.Add(new ILHook(R19CompatibilitySystem.Method(item,"get_"+name),PatchCombatLevel));
        SignatureExperienceCompatibility.Install(hooks, provider);
        Apply();
        Mod.Logger.Info("DINGDONG_SIGNATURE_BALANCE|combatCeiling=100|levelData=preserved|slotProgression=unbounded-positive|extraProjectileProgression=unbounded|penetrationAndLifetime=2x");
    }
    public override void OnWorldLoad() { SignatureExperienceCompatibility.Reset(); Apply(force: true); }
    public override void OnWorldUnload() => SignatureExperienceCompatibility.Reset();
    public override void PreUpdateWorld() => Apply();
    private static void Apply(bool force = false)
    {
        if (config == null) return;
        object current = config();
        int stage = Stage;
        if (!force && ReferenceEquals(current, appliedConfig) && stage == appliedStage) return;
        Set(current, "MaxLevel", 0);
        Set(current, "IncreaseBaseDamage", false); Set(current, "IncreaseFlatDamage", false); Set(current, "IncreaseMultDamage", true);
        // The native spawn damage already includes Weapon Enchantments and
        // Signature. Replacing it on each hit discards provider coefficients.
        Set(current, "UseMinionFix", false);
        Set(current, "DoubleProjMultForAll", false);
        Set(current, "MinLevelForDisableTileCollide", int.MaxValue);
        foreach (string name in new[] { "DamageIncrement", "DefenceIncrement", "CritIncrement", "UseTimeIncrement", "UseAnimationIncrement", "MeleeWeaponSizeIncrement", "ProjectileSizeIncrement", "ProjectileStartVelocityIncrement", "NotUseAmmoChanceIncrement", "WeaponManaCostIncrement" })
            Set(current, name, true);
        Set(current, "MinionSlotsReduceIncrement", true);
        Set(current, "AdditionalProjectileChanceIncrement", true);
        Set(current, "ProjectilePenetrateIncrement", true); Set(current, "ProjectileLifeTimeIncrement", true);
        Stat(current, "Damage", .5f, stage / 2);
        Stat(current, "Defence", .25f, Math.Max(1, stage / 4));
        Stat(current, "Crit", .1f, Math.Max(1, stage / 10));
        Stat(current, "UseTime", .13f, Math.Max(1, stage * 13 / 100));
        Stat(current, "UseAnimation", .13f, Math.Max(1, stage * 13 / 100));
        foreach (string name in new[] { "MeleeSize", "ProjectileSize", "ProjectileStartVelocity", "NotUseAmmoChance", "ManaCost" })
            Stat(current, name, .25f, Math.Max(1, stage / 4));
        Stat(current, "MinionSlotsReduce", .5f, 0);
        // Preserve the provider's original per-level quantity growth. Only its
        // optional ceiling is removed; quantity is independent of combat caps.
        object extra=current.GetType().GetField("AdditionalProjectileOnShootChance").GetValue(current);
        Set(extra,"Max",0);
        Stat(current, "ProjectilePenetrate", 1f, stage);
        Stat(current, "ProjectileLifeTime", 2f, stage * 2);
        appliedConfig = current; appliedStage = stage;
    }
    private static void Set(object target, string name, object value) =>
        (target.GetType().GetField(name) ?? throw new MissingFieldException(target.GetType().FullName, name)).SetValue(target, value);
    private static void Stat(object target, string name, float perLevel, int maximum)
    {
        object stat = target.GetType().GetField(name)?.GetValue(target) ?? throw new MissingFieldException(name);
        Set(stat, "PerLevel", perLevel); Set(stat, "PerLevelMult", 1); Set(stat, "Max", maximum);
    }
    private static void PatchDiscount(ILContext il)
    {
        var c = new ILCursor(il);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<object, float>>(state => SignatureBalanceRules.SlotDiscountPercent(raw(state)));
        c.Emit(OpCodes.Ret);
    }
    private static void PatchCombatLevel(ILContext il)
    {
        var calls=il.Body.Instructions.Where(i=>i.Operand is MethodReference m && m.Name=="get_Level" && m.DeclaringType.FullName=="SignatureEquipment.SignatureGlobalItem").ToArray();
        if(calls.Length!=1)throw new InvalidOperationException("Signature raw combat-stat level contract changed: "+il.Method.Name);
        var c=new ILCursor(il);c.Goto(calls[0],MoveType.After);
        c.EmitDelegate<Func<int,int>>(level=>SignatureBalanceRules.CombatLevel(level,Stage));
    }
    private static void PatchHitDamageWrites(ILContext il)
    {
        var writes=il.Body.Instructions.Where(i=>i.OpCode==OpCodes.Stfld && i.Operand is FieldReference f && f.DeclaringType.FullName=="Terraria.Projectile" && f.Name=="damage").ToArray();
        if(writes.Length!=3)throw new InvalidOperationException("Signature persistent hit-damage writes changed.");
        foreach(var write in writes)
        {
            var c=new ILCursor(il);c.Goto(write,MoveType.AfterLabel);
            c.Emit(OpCodes.Pop);c.Emit(OpCodes.Pop);c.Remove();
        }
    }
    private static void PatchDefence(ILContext il)
    {
        var calls = il.Body.Instructions.Where(i => i.Operand is MethodReference m &&
            m.DeclaringType.FullName == "SignatureEquipment.SignatureGlobalItem" && m.Name is "get_DefenceCapped" or "get_DefenceNotCapped").ToArray();
        if (calls.Length != 2) throw new InvalidOperationException("Signature defence contract changed.");
        foreach (var call in calls)
        {
            var c = new ILCursor(il); c.Goto(call, MoveType.AfterLabel);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<object, Item, int>>((state, item) => SignatureBalanceRules.Defence(item.defense, raw(state), Stage));
            c.Remove();
        }
    }
    private static void PatchSpawn(ILContext il)
    {
        var returns = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
        var c = new ILCursor(il);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate<Action<Projectile>>(p => p.GetGlobalProjectile<SignatureProjectileLimits>().Capture(p));
        foreach (var ret in returns)
        {
            c.Goto(ret, MoveType.AfterLabel);
            c.Emit(OpCodes.Ldarg_0); c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<object, Projectile>>((state, p) => {
                Item item = projectileItem(state);
                if (State(item) != null) p.GetGlobalProjectile<SignatureProjectileLimits>().Normalize(p, RawLevel(item));
            });
        }
    }
    public override void Unload()
    {
        for (int i=hooks.Count-1;i>=0;i--) hooks[i].Dispose();
        hooks.Clear(); template=null;raw=xp=xpNeeded=null;config=null;projectileItem=null;appliedConfig=null;appliedStage=-1;
        SignatureExperienceCompatibility.Reset();
    }
}
