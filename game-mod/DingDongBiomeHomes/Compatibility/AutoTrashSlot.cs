using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Compatibility;

// Retain the provider's saved rules, recovery item, sales and pickup processor.
internal static class AutoTrashSlot
{
 private static ModPlayer template;
 private static FieldInfo enabled, filters, item, sellValue;
 private static MethodInfo sold, sellInstead;
 private static object clientConfig, serverConfig;
 internal static bool Installed => template != null;
 internal static void Bind(Type type) {
  const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
  template = ModLoader.Mods.SelectMany(m => m.GetContent<ModPlayer>()).Single(p => p.GetType() == type);
  enabled = type.GetField("AutoTrashEnabled", flags); filters = type.GetField("AutoTrashItems", flags);
  item = type.GetField("LastAutoTrashItem", flags); sold = type.GetMethod("OnItemAutotrashed", flags);
  object Config(Type t) => typeof(ModContent).GetMethods().Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(t).Invoke(null, null);
  Type client = type.Assembly.GetType("AutoTrash.AutoTrashClientConfig", true), server = type.Assembly.GetType("AutoTrash.AutoTrashServerConfig", true);
  clientConfig = Config(client); serverConfig = Config(server);
  sellInstead = client.GetProperty("SellInstead", flags).GetMethod; sellValue = server.GetField("SellValue", flags);
 }
 private static ModPlayer State(Player player) => player.GetModPlayer(template);
 internal static bool Enabled(Player player) => (bool)enabled.GetValue(State(player));
 internal static void SetEnabled(Player player, bool value) => enabled.SetValue(State(player), value);
 internal static List<Item> Filters(Player player) => (List<Item>)filters.GetValue(State(player));
 internal static Item Item(Player player) => (Item)item.GetValue(State(player));
 internal static void SetItem(Player player, Item value) => item.SetValue(State(player), value);
 internal static bool Click(Player player) {
  if (Main.mouseItem.favorited) return false;
  var state = State(player); Item previous = Item(player);
  bool selling = (bool)sellInstead.Invoke(clientConfig, null);
  // Match the native sale/refund contract when taking back the recovery stack.
  if (selling && Main.mouseItem.IsAir && !previous.IsAir) {
   int cost = (int)Math.Floor((double)previous.value * previous.stack * Math.Max(1, (int)sellValue.GetValue(serverConfig)) / 100);
   if (!player.BuyItem(cost)) return false;
  }
  Item[] slot = { previous };
  ItemSlot.LeftClick(slot, ItemSlot.Context.TrashItem, 0);
  SetItem(player, slot[0]);
  var rules = Filters(player);
  // Rules keep the prefix too (AutoTrashPrefixGuard): a Legendary blade is its own rule.
  if (slot[0].IsAir) rules.RemoveAll(i => i.type == previous.type && i.prefix == previous.prefix);
  else if (!rules.Any(i => i.type == slot[0].type && i.prefix == slot[0].prefix))
  {
   var rule = new Item(slot[0].type);
   if (slot[0].prefix > 0) rule.Prefix(slot[0].prefix);
   rules.Add(rule);
  }
  if (!ReferenceEquals(previous, slot[0]) && !slot[0].IsAir) sold.Invoke(state, null);
  Recipe.FindRecipes();
  return true;
 }
 internal static void Unload() { template = null; enabled = filters = item = sellValue = null; sold = sellInstead = null; clientConfig = serverConfig = null; }
}
