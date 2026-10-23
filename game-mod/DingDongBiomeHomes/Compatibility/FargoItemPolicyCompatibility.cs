using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// Keep the world's enemy difficulty, while items retain their ordinary behavior.
// No shared Eternity/Masochist flag is changed, even temporarily.
public sealed class FargoItemPolicyCompatibility : ModSystem
{
 private readonly List<ILHook> hooks = new();
 private Type configType;
 internal int HookCount => hooks.Count;
 private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

 public override void Load()
 {
  if (!ModLoader.TryGetMod("FargowiltasSouls", out Mod mod)) return;
  if (mod.Version != new Version(1, 7, 3, 9) || mod.Code.ManifestModule.ModuleVersionId != new Guid("535d0777-d3df-4436-ac3f-e1d1fd0ad83b"))
   throw new InvalidOperationException("Fargo item policy provider changed; item and enemy rules must be reviewed separately.");
  try {
   Type Type(string name) => mod.Code.GetType("FargowiltasSouls." + name, true);
   configType = Type("Core.SoulConfig");
   Type item = Type("Content.Items.EModeGlobalItem");
   foreach (MethodInfo method in item.GetMethods(Flags)) {
    if (method.Name is "Load" or "Unload" or "get_InstancePerEntity") continue;
    if (method.Name == "ModifyWeaponDamage") Noop(method);
    else ModeOff(method);
   }
   ModeOff(R19CompatibilitySystem.Method(Type("Content.Items.EmodeItemBalance"), "EmodeBalancePerID"));
   MethodInfo sword = R19CompatibilitySystem.Method(Type("Content.Items.SwordGlobalItem"), "BroadswordRework");
   hooks.Add(new ILHook(sword, il => { Clear(il); var c = new ILCursor(il); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret); }));
   ModeOff(R19CompatibilitySystem.Method(Type("Content.Projectiles.SpearRework"), "PostAI"));
   Type projectile = Type("Content.Projectiles.EModeGlobalProjectile");
   foreach (MethodInfo method in projectile.GetMethods(Flags).Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Projectile))) {
    hooks.Add(new ILHook(method, il => {
     foreach (Instruction call in il.Body.Instructions.Where(IsMode).ToArray()) {
      var c = new ILCursor(il); c.Goto(call, MoveType.After); c.Emit(OpCodes.Ldarg_1);
      c.EmitDelegate<Func<bool, Projectile, bool>>((mode, p) => mode && (p == null || p.hostile || !p.friendly));
     }
    }));
   }
   Type player = Type("Core.ModPlayers.EModePlayer");
   foreach (string name in new[] { "PostUpdateEquips", "ModifyHitByNPC", "ModifyHitByProjectile" }) ModeOff(R19CompatibilitySystem.Method(player, name));
   Noop(R19CompatibilitySystem.Method(player, "MurderGreaterDangersense"));
   ReplaceRead(R19CompatibilitySystem.Method(player, "PostUpdateBuffs"), "chaosState", OpCodes.Ldc_I4_0);
   ReplaceRead(R19CompatibilitySystem.Method(player, "ModifyHurt"), "brainOfConfusionItem", OpCodes.Ldnull);
   hooks.Add(new ILHook(R19CompatibilitySystem.Method(player, "PostUpdateMiscEffects"), il => {
    Instruction field = il.Body.Instructions.Single(i => i.Operand is FieldReference f && f.Name == "IsAWhip");
    Instruction read = field.Next; while (read != null && read.OpCode != OpCodes.Ldelem_U1) read = read.Next;
    if (read == null) throw new InvalidOperationException("Fargo whip speed policy boundary changed.");
    var c = new ILCursor(il); c.Goto(read, MoveType.After); c.Emit(OpCodes.Pop); c.Emit(OpCodes.Ldc_I4_0);
   }));
   Type main = Type("FargowiltasSouls");
   foreach (string name in new[] { "HorsemansBlade_SpawnPumpkin", "CombinedHooks_ModifyHitNPCWithProj" }) ModeOff(R19CompatibilitySystem.Method(main, name));
   Mod.Logger.Info($"DINGDONG_FARGO_ITEM_POLICY|hooks={hooks.Count}|itemBalance=ordinary|weaponReworks=off|enemyDifficulty=preserved");
  } catch { Unload(); throw; }
 }
 private static bool IsMode(Instruction i) => i.Operand is MethodReference m && m.DeclaringType.FullName == "FargowiltasSouls.Core.Systems.WorldSavingSystem" && m.Name == "get_EternityMode";
 private void ModeOff(MethodInfo method) => hooks.Add(new ILHook(method, il => {
  var sites = il.Body.Instructions.Where(IsMode).ToArray();
  if (sites.Length == 0) throw new InvalidOperationException("Fargo item mode boundary changed: " + method.Name);
  foreach (Instruction site in sites) { var c = new ILCursor(il); c.Goto(site, MoveType.After); c.Emit(OpCodes.Pop); c.Emit(OpCodes.Ldc_I4_0); }
 }));
 private void ReplaceRead(MethodInfo method, string field, OpCode replacement) => hooks.Add(new ILHook(method, il => {
  var sites = il.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f && f.DeclaringType.FullName == "Terraria.Player" && f.Name == field).ToArray();
  if (sites.Length == 0) throw new InvalidOperationException("Fargo equipment policy boundary changed: " + field);
  foreach (Instruction site in sites) { var c = new ILCursor(il); c.Goto(site, MoveType.After); c.Emit(OpCodes.Pop); c.Emit(replacement); }
 }));
 private void Noop(MethodInfo method) => hooks.Add(new ILHook(method, il => { Clear(il); new ILCursor(il).Emit(OpCodes.Ret); }));
 private static void Clear(ILContext il) { il.Body.Instructions.Clear(); il.Body.Variables.Clear(); il.Body.ExceptionHandlers.Clear(); }
 private void EnforceConfig() { if (configType != null) configType.GetField("WeaponReworks", Flags).SetValue(configType.GetField("Instance", Flags).GetValue(null), false); }
 public override void PostSetupContent() => EnforceConfig();
 public override void OnWorldLoad() => EnforceConfig();
 public override void Unload() { for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose(); hooks.Clear(); configType = null; }
}
