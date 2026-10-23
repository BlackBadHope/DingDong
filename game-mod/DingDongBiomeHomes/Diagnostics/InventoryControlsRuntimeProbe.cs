using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace DingDongBiomeHomes.Diagnostics;
internal static class InventoryControlsRuntimeProbe
{
 internal static int Run()
 {
  int checks = 0; void Require(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks++; }
  int oldId = Main.myPlayer; Player oldPlayer = Main.player[0];
  try {
   Player player = new(); player.whoAmI = 0; Main.myPlayer = 0; Main.player[0] = player;
   AutoUncraftPlayer state = player.GetModPlayer<AutoUncraftPlayer>();
   Require(!state.Enabled && state.Filters.Count == 0, "New character starts with empty paused auto-uncraft");
   Item chair = new(ItemID.WoodenChair, 10);
   Require(state.Toggle(chair) && chair.stack == 10 && state.Filters.Count == 1, "Adding a rule never consumes its sample");
   state.Enabled = true; chair.favorited = true;
   Require(!state.Eligible(chair), "Favorited item excluded from auto-uncraft"); chair.favorited = false;
   player.inventory[0] = chair;
   int done = state.ProcessSlice();
   Require(done > 0 && done <= 8 && chair.stack < 10 && player.inventory.Any(i => i.type == ItemID.Wood), "Bounded native auto-uncraft returns wood");
   state.Enabled = false; int count = chair.stack; state.ProcessSlice();
   Require(chair.stack == count, "Paused auto-uncraft preserves its inventory");
   TagCompound saved = new(); state.SaveData(saved); AutoUncraftPlayer loaded = new(); loaded.LoadData(saved);
   Require(!loaded.Enabled && loaded.Filters.Count == 1 && loaded.Filters[0].type == ItemID.WoodenChair, "Rules and enabled state survive player serialization");
   for (int i = 0; i < 50; i++) player.inventory[i] = new Item(ItemID.GoldBar, 9999);
   player.inventory[0] = new Item(ItemID.WoodenChair, 2);
   Require(!UncraftRecipeCompatibility.TryBatch(player.inventory[0], out _) && player.inventory[0].stack == 2 && player.inventory.Skip(1).Take(49).All(i => i.type == ItemID.GoldBar && i.stack == 9999), "Full inventory preserves both input and outputs");
   const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
   Mod improve = ModLoader.GetMod("ImproveGame");
   foreach (string name in new[] { "InventoryTrashGUI", "GarbageListGUI" }) {
    Type type = improve.Code.GetType("ImproveGame.UI.AutoTrash." + name, true);
    object ui = Activator.CreateInstance(type);
    Require(!(bool)type.GetProperty("Enabled", flags).GetValue(ui), "ImproveGame duplicate trash UI disabled: " + name);
   }
   Mod auto = ModLoader.GetMod("AutoTrash"); Type apt = auto.Code.GetType("AutoTrash.AutoTrashPlayer", true);
   var autoState = GetPlayer(player, apt);
   apt.GetField("AutoTrashEnabled", flags).SetValue(autoState, true);
   ((List<Item>)apt.GetField("AutoTrashItems", flags).GetValue(autoState)).Add(new Item(ItemID.WoodenChair));
   MethodInfo shouldTrash = apt.GetMethod("ShouldItemBeTrashed", flags);
   state.Enabled = false; bool nativeTrashes = (bool)shouldTrash.Invoke(autoState, new object[] { chair });
   state.Enabled = true;
   Require(nativeTrashes && !(bool)shouldTrash.Invoke(autoState, new object[] { chair }), "Auto-uncraft takes priority over an overlapping AutoTrash rule");
   Item weapon = new(ItemID.CopperShortsword); var tips = new List<TooltipLine> { new(ModLoader.GetMod("SignatureEquipment"), "Level", "Signature legacy level row") };
   ModLoader.GetMod("DingDongBiomeHomes").GetContent<GlobalItem>().OfType<ProgressionTooltip>().Single().ModifyTooltips(weapon, tips);
   Require(tips.Count(t => t.Name == "SignatureProgress") == 1 && tips.All(t => t.Mod != "SignatureEquipment"), "One combined Signature level and XP row");
   int soulType = ModLoader.GetMod("FargowiltasSouls").Find<ModItem>("EternitySoul").Type;
   Item soul = new(soulType); tips.Clear(); ModLoader.GetMod("DingDongBiomeHomes").GetContent<GlobalItem>().OfType<ProgressionTooltip>().Single().ModifyTooltips(soul, tips);
   Require(!tips.Any(t => t.Name.StartsWith("Signature")), "Soul accessory has no irrelevant weapon mastery lines");
   checks += CheckSoul(player, soul);
   DingDongBiomeHomes.Instance.Logger.Info("DINGDONG_R33_PROBE|step=inventory-and-soul|status=PASS|checks=" + checks);
   checks += PackControls.RuntimeChecks();
   DingDongBiomeHomes.Instance.Logger.Info("DINGDONG_R33_PROBE|step=controls|status=PASS");
   checks += LoggedFailureRuntimeProbe.Run(player);
   DingDongBiomeHomes.Instance.Logger.Info("DINGDONG_R33_PROBE|step=logged-failures|status=PASS");
   checks += DummyRuntimeProbe.Run(player);
   DingDongBiomeHomes.Instance.Logger.Info("DINGDONG_R33_PROBE|step=dummy-callbacks|status=PASS");
   checks += AutoUncraftUI.GraphicsProbe();
  } finally { Main.myPlayer = oldId; Main.player[0] = oldPlayer; }
  return checks;
 }
 private static object GetPlayer(Player player, Type type) => player.GetModPlayer(ModLoader.Mods.SelectMany(m => m.GetContent<ModPlayer>()).Single(p => p.GetType() == type));

 private static int CheckSoul(Player player, Item soul)
 {
  int checks = 0; void Require(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks++; }
  Mod fargo = ModLoader.GetMod("FargowiltasSouls");
  const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
  object state = GetPlayer(player, fargo.Code.GetType("FargowiltasSouls.Core.ModPlayers.FargoSoulsPlayer", true));
  Type t = state.GetType(); FieldInfo stack = t.GetField("TinEternityDamage", flags), eternity = t.GetField("Eternity", flags);
  player.statLifeMax2 = 100; player.maxMinions = 1; player.maxTurrets = 1; player.GetDamage(DamageClass.Generic) = StatModifier.Default;
  soul.ModItem.UpdateAccessory(player, false);
  Require((bool)eternity.GetValue(state) && player.maxMinions >= 21 && player.maxTurrets >= 11, "Native Eternity Soul enables its own additional minions and sentries");
  Require(player.statLifeMax2 >= 500 && player.GetDamage(DamageClass.Generic).Additive >= 3.5f, "Native Soul contributes x5 life and at least +250 percent additive damage");
  Type effectType = fargo.Code.GetType("FargowiltasSouls.Content.Items.Accessories.Enchantments.TinEffect", true);
  object effect = Activator.CreateInstance(effectType);
  stack.SetValue(state, 100f);
  effectType.GetMethod("PostUpdateEquips", flags).Invoke(effect, new object[] { player });
  Require((float)stack.GetValue(state) == 47.5f, "Native Tin stacking caps at +4750 percent damage");
  effectType.GetMethod("TinHurt", flags).Invoke(null, new object[] { player, true });
  Require((float)stack.GetValue(state) == 0f, "Native Tin stacking resets on taking damage");
  Type builderType = fargo.Code.GetType("FargowiltasSouls.Content.Items.Accessories.Souls.BuilderEffect", true);
  builderType.GetMethod("PostUpdateEquips", flags).Invoke(Activator.CreateInstance(builderType), new object[] { player });
  Require((bool)t.GetField("BuilderMode", flags).GetValue(state), "Soul's native builder effect activates material preservation");
  Item crate = new(ItemID.FloatingIslandFishingCrate);
  GlobalItem fargoItem = fargo.GetContent<GlobalItem>().Single(g => g.GetType().FullName == "FargowiltasSouls.Content.Items.FargoGlobalItem");
  bool building = fargoItem.ConsumeItem(crate, player), opening = false;
  FargoCrateCompatibility.DuringOpening(() => opening = fargoItem.ConsumeItem(crate, player));
  Require(crate.createTile >= 0 && !building && opening, "Builder effect preserves crate placement but no longer prevents consuming opened loot");
  Item block = new(ItemID.StoneBlock); bool preserved = false;
  FargoCrateCompatibility.DuringOpening(() => preserved = !fargoItem.ConsumeItem(block, player));
  Require(preserved, "Fargo's ordinary building-material preservation remains intact");
  Item[] worldItems = Main.item.ToArray(); int split = Main.stackSplit; bool release = Main.mouseRightRelease;
  try {
   for (int i = 0; i < Main.item.Length; i++) Main.item[i] = new Item();
   Item actual = new(ItemID.FloatingIslandFishingCrate, 2);
   typeof(Terraria.UI.ItemSlot).GetMethod("TryOpenContainer", flags).Invoke(null, new object[] { actual, player });
   Require(actual.stack == 1, "Actual native crate opening consumes one crate with Soul BuilderMode active");
  } finally {
   for (int i = 0; i < worldItems.Length; i++) Main.item[i] = worldItems[i]; Main.stackSplit = split; Main.mouseRightRelease = release;
  }
  return checks;
 }
}
