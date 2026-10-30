using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Compatibility;

public sealed class AutoUncraftPlayer : ModPlayer
{
 internal bool Enabled;
 internal List<Item> Filters = new();
 internal Item LastItem = new();
 internal Item[] Input = { new Item() };
 internal string LastError = "";
 private int scan, cooldown;
 public override void Initialize() { Enabled = false; Filters = new(); LastItem = new(); Input = new[] { new Item() }; scan = cooldown = 0; }
 // Owner, 27.10: a rule is the item together with its prefix, so a plain sword on the list never
 // takes a Legendary one with it (and the reverse).
 private static bool Same(Item rule, Item item) => rule.type == item.type && rule.prefix == item.prefix;
 private static Item Rule(Item item) { var rule = new Item(item.type); if (item.prefix > 0) rule.Prefix(item.prefix); return rule; }
 internal void AddRule(Item item) {
  if (item != null && !item.IsAir && !Includes(item) && UncraftRecipeCompatibility.BasicRecipe(item) != null)
   Filters.Add(Rule(item));
 }
 internal bool Includes(Item item) => item != null && !item.IsAir && Filters.Any(i => Same(i, item));
 internal bool Eligible(Item item) => Enabled && item != null && !item.IsAir && !item.favorited && Includes(item);
 internal bool Toggle(Item item)
 {
  if (item == null || item.IsAir) return false;
  int existing = Filters.FindIndex(i => Same(i, item));
  if (existing >= 0) { Filters.RemoveAt(existing); return true; }
  if (UncraftRecipeCompatibility.BasicRecipe(item) == null) return false;
  Filters.Add(Rule(item)); return true;
 }
 public override void SaveData(TagCompound tag)
 {
  tag["AutoUncraftEnabled"] = Enabled;
  tag["AutoUncraftFilters"] = Filters.Select(ItemIO.Save).ToList();
  tag["AutoUncraftInput"] = ItemIO.Save(Input[0]);
  if (AutoTrashSlot.Installed) tag["AutoTrashRecovery"] = ItemIO.Save(AutoTrashSlot.Item(Player));
 }
 public override void LoadData(TagCompound tag)
 {
  Enabled = tag.GetBool("AutoUncraftEnabled"); Filters = new();
  Input = new[] { tag.ContainsKey("AutoUncraftInput") ? ItemIO.Load(tag.GetCompound("AutoUncraftInput")) : new Item() };
  if (AutoTrashSlot.Installed && tag.ContainsKey("AutoTrashRecovery")) AutoTrashSlot.SetItem(Player, ItemIO.Load(tag.GetCompound("AutoTrashRecovery")));
  if (tag.ContainsKey("AutoUncraftFilters"))
   foreach (TagCompound entry in tag.GetList<TagCompound>("AutoUncraftFilters")) {
    Item item = ItemIO.Load(entry); if (!item.IsAir && !Filters.Any(i => Same(i, item))) Filters.Add(item);
   }
 }
 public override void PostUpdate()
 {
  if (Main.dedServ || Player.whoAmI != Main.myPlayer || Main.gameMenu || Player.dead || !Enabled || Filters.Count == 0) return;
  if (++cooldown < 6) return;
  cooldown = 0; ProcessSlice();
 }
 internal int ProcessSlice()
 {
  // Native UncraftItem performs one atomic recipe batch and restores the inventory on failure.
  // Bound both calls and wall time; never drain an unbounded stack on the game thread.
  var timer = Stopwatch.StartNew(); int completed = 0;
  if (Eligible(Input[0])) {
   if (UncraftRecipeCompatibility.TryBatch(Input[0], out string reason)) { completed++; LastError = ""; }
   else if (!string.IsNullOrEmpty(reason)) LastError = reason;
  }
  for (int visited = 0; visited < 50 && completed < 8; visited++) {
   int index = scan++ % 50; Item item = Player.inventory[index];
   if (Eligible(item)) {
    Item preview = new Item(item.type);
    if (UncraftRecipeCompatibility.TryBatch(item, out string reason)) { LastItem = preview; completed++; LastError = ""; }
    else if (!string.IsNullOrEmpty(reason)) LastError = reason;
   }
   if (timer.Elapsed.TotalMilliseconds >= 1.5) break;
  }
  if (completed > 0) Recipe.FindRecipes();
  return completed;
 }
}
