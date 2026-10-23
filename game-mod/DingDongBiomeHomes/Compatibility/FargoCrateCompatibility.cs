using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
namespace DingDongBiomeHomes.Compatibility;
public sealed class FargoCrateCompatibility : ModSystem
{
 private Hook opening; private ILHook consume;
 [ThreadStatic] private static int openingDepth;
 private delegate void OriginalOpen(Item item, Player player);
 private delegate void OpenHook(OriginalOpen original, Item item, Player player);
 public override void Load()
 {
  if (Main.dedServ || !ModLoader.TryGetMod("FargowiltasSouls", out Mod mod)) return;
  if (mod.Version != new Version(1, 7, 3, 9)) throw new InvalidOperationException("Fargo crate consumption provider changed.");
  MethodInfo open = typeof(ItemSlot).GetMethod("TryOpenContainer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Item), typeof(Player) }, null)
   ?? throw new InvalidOperationException("Native container opening boundary changed.");
  opening = new Hook(open, new OpenHook((original, item, player) => DuringOpening(() => original(item, player))));
  Type type = mod.Code.GetType("FargowiltasSouls.Content.Items.FargoGlobalItem", true);
  consume = new ILHook(R19CompatibilitySystem.Method(type, "ConsumeItem"), il => {
   Instruction original = il.Body.Instructions[0]; var c = new ILCursor(il);
   c.Emit(OpCodes.Ldarg_1); c.EmitDelegate<Func<Item, bool>>(LootContainerOpening); c.Emit(OpCodes.Brfalse, original);
   c.Emit(OpCodes.Ldc_I4_1); c.Emit(OpCodes.Ret);
  });
 }
 private static bool LootContainerOpening(Item item) => openingDepth > 0 && item != null && !item.IsAir && item.createTile >= 0 &&
  ItemLoader.CanRightClick(item) && Main.ItemDropsDB.GetRulesForItemID(item.type).Count > 0;
 internal static void DuringOpening(Action action) { openingDepth++; try { action(); } finally { openingDepth--; } }
 public override void Unload() { consume?.Dispose(); opening?.Dispose(); consume = null; opening = null; openingDepth = 0; }
}
