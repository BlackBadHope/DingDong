using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Compatibility;

internal static class LuiafkBuildingCompatibility
{
 private static FieldInfo item;
 internal static void Install(List<ILHook> hooks) {
  Type type = R19CompatibilitySystem.Provider("miningcracks_take_on_luiafk", "1.2.5", "miningcracks_take_on_luiafk.Images.Projectiles.BuildingProjectile");
  if (type == null) return;
  if (type.Module.ModuleVersionId != Guid.Parse("aee00a10-6fb2-4a00-a082-bed9a648e375")) throw new InvalidOperationException("Luiafk building cursor identity changed.");
  item = type.GetField("item", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
  if (item == null) throw new MissingFieldException(type.FullName, "item");
  hooks.Add(new ILHook(R19CompatibilitySystem.Method(type, "AI"), il => {
   Instruction original = il.Body.Instructions[0]; var c = new ILCursor(il);
   c.Emit(OpCodes.Ldarg_0); c.EmitDelegate<Func<ModProjectile, bool>>(CanRunLocalAI);
   c.Emit(OpCodes.Brtrue, original); c.Emit(OpCodes.Ret);
  }));
 }
 private static bool CanRunLocalAI(ModProjectile self) {
  Projectile p = self.Projectile;
  if ((uint)p.owner >= Main.maxPlayers || Main.player[p.owner] == null || !Main.player[p.owner].active) { p.hide = true; return false; }
  if (Main.netMode == NetmodeID.Server || p.owner != Main.myPlayer) {
   // Only the owner has a mouse and EntitySource_ItemUse. Network spawn sources
   // cannot populate Luiafk's private item; preserve the cursor until its owner removes it.
   p.hide = true; p.timeLeft++; return false;
  }
  if (item.GetValue(self) is not Item) { p.hide = true; return false; }
  return true;
 }
 internal static void Reset() => item = null;
}
