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

public sealed class InventoryTrashCompatibility : ModSystem
{
 private readonly List<ILHook> hooks = new();
 public override void Load()
 {
  if (Main.dedServ) return;
  Type type = R19CompatibilitySystem.Provider("ImproveGame", "1.8.2", "ImproveGame.UI.AutoTrash.InventoryTrashGUI");
  if (type == null || type.Module.ModuleVersionId != Guid.Parse("b3326788-dd23-4981-b017-a9b659007680"))
   throw new InvalidOperationException("Duplicate trash provider identity changed.");
  // Disable only ImproveGame's duplicate trash feature, including pickup and Ctrl-click deletion.
  foreach (Type t in type.Assembly.GetTypes())
   foreach (MethodInfo method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
    if (method.GetMethodBody() == null) continue;
    if ((t.FullName == "ImproveGame.UI.AutoTrash.InventoryTrashGUI" || t.FullName == "ImproveGame.UI.AutoTrash.GarbageListGUI") && method.Name == "get_Enabled" ||
     t.FullName == "ImproveGame.UI.AutoTrash.AutoTrashPlayer" && method.Name == "ShiftClickSlot")
     hooks.Add(new ILHook(method, il => { Clear(il); var c = new ILCursor(il); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret); }));
    else if (method.Name == "PressAutoTrashKeybind")
     hooks.Add(new ILHook(method, il => { Clear(il); new ILCursor(il).Emit(OpCodes.Ret); }));
    else if (t.FullName == "ImproveGame.Common.GlobalItems.GrabAndPickup" && method.Name == "OnPickup")
     hooks.Add(new ILHook(method, il => {
      var c = new ILCursor(il); if (!c.TryGotoNext(MoveType.After, i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f && f.Name == "QoLAutoTrash"))
       throw new InvalidOperationException("ImproveGame trash pickup gate changed.");
      c.Emit(OpCodes.Pop); c.Emit(OpCodes.Ldc_I4_0);
     }));
   }
  Type auto = R19CompatibilitySystem.Provider("AutoTrash", "0.8.1.1", "AutoTrash.AutoTrashPlayer");
  Type frontend = auto.Assembly.GetType("AutoTrash.AutoTrashGlobalItem", true);
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(frontend, "DrawUpdateAutoTrash"), il => { Clear(il); new ILCursor(il).Emit(OpCodes.Ret); }));
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(auto, "ShouldItemBeTrashed"), il => {
   var c = new ILCursor(il); Instruction original = il.Body.Instructions[0];
   c.Emit(OpCodes.Ldarg_1); c.EmitDelegate<Func<Item, bool>>(item => Main.LocalPlayer.GetModPlayer<AutoUncraftPlayer>().Eligible(item));
   c.Emit(OpCodes.Brfalse, original); c.Emit(OpCodes.Ldc_I4_0); c.Emit(OpCodes.Ret);
  }));
 }
 public override void PostSetupContent() {
  if (!Main.dedServ) AutoTrashSlot.Bind(ModLoader.GetMod("AutoTrash").Code.GetType("AutoTrash.AutoTrashPlayer", true));
 }
 private static void Clear(ILContext il) { il.Body.Instructions.Clear(); il.Body.Variables.Clear(); il.Body.ExceptionHandlers.Clear(); }
 public override void Unload() { for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose(); hooks.Clear(); AutoTrashSlot.Unload(); }
}
