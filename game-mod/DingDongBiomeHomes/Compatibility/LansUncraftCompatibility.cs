using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Compatibility;

internal static class LansUncraftCompatibility
{
 private static FieldInfo visible;
 internal static void Install(List<ILHook> hooks)
 {
  if (Main.dedServ) return;
  Type type = R19CompatibilitySystem.Provider("LansUncraftItems", "2023.10.8.1", "LansUncraftItems.Panel");
  if (type == null) return;
  if (type.Module.ModuleVersionId != Guid.Parse("fda6c0ae-fb0e-44e4-80cf-2e32af4b7f72"))
   throw new InvalidOperationException("Uncraft slot provider identity changed.");
  visible = type.GetField("visible", BindingFlags.Static | BindingFlags.Public);
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "DrawSelf"), il => {
   il.Body.Instructions.Clear(); il.Body.ExceptionHandlers.Clear(); il.Body.Variables.Clear();
   var c = new ILCursor(il); c.Emit(OpCodes.Ldarg_0);
   c.EmitDelegate<Action<UIState>>(HideOriginal); c.Emit(OpCodes.Ret);
  }));
 }
 private static void HideOriginal(UIState state) { visible.SetValue(null, false); state.RemoveAllChildren(); }
 internal static void Reset() => visible = null;
 internal static int RunRuntimeChecks()
 {
  if (Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_UI_LAYOUT_PROBE") != "1")
   throw new InvalidOperationException("Explicit disposable UI probe required.");
  if (visible == null || !UncraftRecipeCompatibility.Installed) throw new InvalidOperationException("Slot backend missing.");
  return AutoUncraftUI.RunRuntimeChecks();
 }
}
