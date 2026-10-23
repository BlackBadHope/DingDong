using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
namespace DingDongBiomeHomes.Compatibility;
public sealed class AutoUncraftUI : ModSystem
{
 private static bool showList; private static int page; private static Func<int> autoX, autoY;
 internal static string Text(string key, params object[] args) => Language.GetTextValue("Mods.DingDongBiomeHomes.AutoUncraft." + key, args);
 public override void PostSetupContent() => EnsureAnchor();
 private static void EnsureAnchor() {
  if (autoX != null && autoY != null) return;
  if (Main.dedServ || !ModLoader.TryGetMod("AutoTrash", out Mod auto)) return;
  Type type = auto.Code.GetType("AutoTrash.AutoTrashClientConfig", true);
  object config = typeof(ModContent).GetMethods().Single(m => m.Name == "GetInstance" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(type).Invoke(null, null);
  autoX = () => (int)type.GetField("SlotPositionX").GetValue(config); autoY = () => (int)type.GetField("SlotPositionY").GetValue(config);
 }
 public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
  int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
  if (index >= 0) layers.Insert(index, new LegacyGameInterfaceLayer("DingDong: Auto Uncraft", Draw, InterfaceScaleType.UI));
 }
 internal static Rectangle SlotBounds(int width, int height, int bottom, bool chest, int ox = 8, int oy = 5) {
  float scale = chest ? .755f : .85f;
  int x = (chest ? 453 : 448) - (int)(56 * scale) + (int)((ox - 8) * 56 * scale) - 100;
  int y = bottom + (chest ? 168 : 0) + (int)((oy - 5) * 56 * scale) - 2;
  return new Rectangle(Math.Clamp(x, 4, Math.Max(4, width - 140)), Math.Clamp(y, 4, Math.Max(4, height - 56)), 40, 40);
 }
 private static bool Draw() {
  if (!Main.playerInventory || Main.gameMenu || PlayerInput.IgnoreMouseInterface || !UncraftRecipeCompatibility.Installed) { showList = false; return true; }
  SpriteBatch batch = Main.spriteBatch;
  int width = (int)(Main.screenWidth / Main.UIScale), height = (int)(Main.screenHeight / Main.UIScale);
  Rectangle slot = SlotBounds(width, height, Main.instance.invBottom, (Main.LocalPlayer.chest != -1 && !Main.recBigList) || Main.npcShop > 0, autoX?.Invoke() ?? 8, autoY?.Invoke() ?? 5);
  AutoUncraftPlayer player = Main.LocalPlayer.GetModPlayer<AutoUncraftPlayer>();
  DrawAutomation(batch, player, slot, false);
  Rectangle trash = new(slot.X + 72, slot.Y, 40, 40);
  DrawAutomation(batch, player, trash, true);
  if (showList) DrawList(batch, player, slot, width, height);
  return true;
 }
 private static bool trashList;
 private static void DrawAutomation(SpriteBatch batch, AutoUncraftPlayer player, Rectangle slot, bool trash) {
  Item[] items = trash ? new[] { AutoTrashSlot.Item(Main.LocalPlayer) } : player.Input;
  DrawSlotBackground(batch, slot);
  float old = Main.inventoryScale;
  try {
   Main.inventoryScale = 40f / TextureAssets.InventoryBack.Value.Width;
   ItemSlot.Draw(batch, items, ItemSlot.Context.BankItem, 0, slot.TopLeft());
   if (items[0].IsAir) {
    if (trash) batch.Draw(TextureAssets.Trash.Value, new Rectangle(slot.X + 9, slot.Y + 8, 22, 24), Color.Silver * .7f);
    else Utils.DrawBorderString(batch, "[i:" + ItemID.WoodenHammer + "]", slot.TopLeft() + new Vector2(6, 5), Color.White * .7f, .8f);
   }
   if (Hovered(slot)) {
    Main.HoverItem = items[0].Clone();
    Main.hoverItemName = Text(trash ? "TrashSlotHint" : "SlotHint");
    if (!trash && !Main.mouseItem.IsAir) Main.hoverItemName += "\n" + Preview(Main.mouseItem);
    if (Main.mouseLeft && Main.mouseLeftRelease) {
     if (trash) AutoTrashSlot.Click(Main.LocalPlayer);
     else TransferUncraft(player, false);
     Main.mouseLeftRelease = false;
    }
    if (!trash && Main.mouseRight && Main.mouseRightRelease) {
     TransferUncraft(player, true); Main.mouseRightRelease = false;
    }
   }
  } finally { Main.inventoryScale = old; }
  bool enabled = trash ? AutoTrashSlot.Enabled(Main.LocalPlayer) : player.Enabled;
  List<Item> filters = trash ? AutoTrashSlot.Filters(Main.LocalPlayer) : player.Filters;
  Rectangle toggle = new(slot.Right + 3, slot.Top, 22, 20), list = new(slot.Right + 3, slot.Top + 21, 22, 20);
  batch.Draw((enabled ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff).Value, toggle, Color.White);
  batch.Draw(TextureAssets.InfoIcon[5].Value, list, Color.White);
  if (Hovered(toggle)) {
   Main.hoverItemName = Text((trash ? "Trash" : "") + (enabled ? "Enabled" : "Disabled"), filters.Count);
   if (LeftClick()) { if (trash) AutoTrashSlot.SetEnabled(Main.LocalPlayer, !enabled); else player.Enabled = !enabled; Click(); }
  }
  if (Hovered(list)) {
   Main.hoverItemName = Text(trash ? "TrashShowList" : "ShowList");
   if (LeftClick()) { showList = !showList || trashList != trash; trashList = trash; page = 0; Click(); }
  }
 }
 internal static bool TransferUncraft(AutoUncraftPlayer player, bool right) {
  if (!Main.mouseItem.IsAir && (Main.mouseItem.favorited || UncraftRecipeCompatibility.BasicRecipe(Main.mouseItem) == null)) {
   Main.NewText(Text(Main.mouseItem.favorited ? "FavoriteHint" : "CannotUncraft")); return false;
  }
  if (right) ItemSlot.RightClick(player.Input, ItemSlot.Context.BankItem, 0);
  else ItemSlot.LeftClick(player.Input, ItemSlot.Context.BankItem, 0);
  player.AddRule(player.Input[0]);
  Recipe.FindRecipes();
  return true;
 }
 private static string Preview(Item item) {
  Recipe recipe = UncraftRecipeCompatibility.BasicRecipe(item);
  if (recipe == null) return Text("CannotUncraft");
  return Text("Preview", recipe.createItem.stack, item.Name, string.Join(", ", recipe.requiredItem.Select(i => i.Name + " x" + i.stack))) + "\n" + Text("RatioHint");
 }
 private static void DrawList(SpriteBatch batch, AutoUncraftPlayer player, Rectangle slot, int width, int height) {
  List<Item> filters = trashList ? AutoTrashSlot.Filters(Main.LocalPlayer) : player.Filters;
  int pw = Math.Min(460, width - 8), ph = Math.Min(330, height - 8);
  Rectangle panel = new(Math.Clamp(slot.X - 280, 4, Math.Max(4, width - pw - 4)), Math.Clamp(slot.Bottom + 16, 4, Math.Max(4, height - ph - 4)), pw, ph);
  batch.Draw(TextureAssets.MagicPixel.Value, panel, new Color(23, 38, 68, 245)); Hovered(panel);
  Utils.DrawBorderString(batch, Text(trashList ? "TrashTitle" : "Title", filters.Count), panel.TopLeft() + new Vector2(12, 10), Color.LightCyan, .85f);
  Utils.DrawBorderString(batch, Text("EditHint"), panel.TopLeft() + new Vector2(12, 40), Color.White, .66f);
  Utils.DrawBorderString(batch, Text(trashList ? "TrashSafetyHint" : "SafetyHint"), panel.TopLeft() + new Vector2(12, 61), Color.Silver, .63f);
  Button(batch, new Rectangle(panel.Right - 29, panel.Top + 6, 22, 24), "X", () => showList = false);
  int pages = Math.Max(1, (filters.Count + 39) / 40); page = Math.Clamp(page, 0, pages - 1);
  float old = Main.inventoryScale;
  try {
   Main.inventoryScale = .68f;
   for (int n = page * 40; n < Math.Min(filters.Count, (page + 1) * 40); n++) {
    int i = n - page * 40; Rectangle box = new(panel.X + 12 + i % 10 * 43, panel.Y + 94 + i / 10 * 44, 38, 38);
    Item item = filters[n]; DrawSlotBackground(batch, box); ItemSlot.Draw(batch, new[] { item }, ItemSlot.Context.BankItem, 0, box.TopLeft());
    if (Hovered(box)) { Main.hoverItemName = item.Name + "\n" + (trashList ? "" : Preview(item)) + "\n" + Text("RemoveHint"); if (LeftClick()) { filters.RemoveAt(n); Click(); break; } }
   }
  } finally { Main.inventoryScale = old; }
  Button(batch, new Rectangle(panel.X + 12, panel.Bottom - 34, 35, 26), "<", () => page = Math.Max(0, page - 1));
  Button(batch, new Rectangle(panel.Right - 47, panel.Bottom - 34, 35, 26), ">", () => page = Math.Min(pages - 1, page + 1));
  Utils.DrawBorderString(batch, (page + 1) + " / " + pages, new Vector2(panel.X + panel.Width / 2 - 20, panel.Bottom - 30), Color.White, .75f);
 }
 private static void Button(SpriteBatch batch, Rectangle box, string text, Action action) {
  bool hover = Hovered(box); batch.Draw(TextureAssets.MagicPixel.Value, box, hover ? new Color(75, 105, 140) : new Color(42, 62, 95));
  Utils.DrawBorderString(batch, text, box.TopLeft() + new Vector2(6, 1), Color.White, .8f);
  if (hover && LeftClick()) { action(); Click(); }
 }
 private static void DrawSlotBackground(SpriteBatch batch, Rectangle box) {
  batch.Draw(TextureAssets.MagicPixel.Value, box, new Color(91, 120, 155));
  Rectangle inner = box; inner.Inflate(-2, -2);
  batch.Draw(TextureAssets.MagicPixel.Value, inner, new Color(34, 49, 82));
 }
 private static bool Hovered(Rectangle box) {
  if (!box.Contains((new Vector2(Main.mouseX, Main.mouseY) / Main.UIScale).ToPoint())) return false;
  Main.LocalPlayer.mouseInterface = true; Main.LocalPlayer.cursorItemIconEnabled = false; return true;
 }
 private static bool LeftClick() { if (!Main.mouseLeft || !Main.mouseLeftRelease) return false; Main.mouseLeftRelease = false; return true; }
 private static void Click() => SoundEngine.PlaySound(SoundID.MenuTick);
 internal static int RunRuntimeChecks() {
  EnsureAnchor();
  if (autoX == null || autoY == null) throw new InvalidOperationException("AutoTrash anchor missing.");
  foreach (int width in new[] { 854, 1280, 1920 }) foreach (bool chest in new[] { false, true }) {
   Rectangle slot = SlotBounds(width, 720, 258, chest);
   if (slot.X < 0 || slot.Right + 97 > width || slot.Bottom > 720) throw new InvalidOperationException("Auto-uncraft slot outside viewport.");
  }
  return 6;
 }
 internal static int GraphicsProbe()
 {
  string output = Environment.GetEnvironmentVariable("DINGDONG_AUTO_UNCRAFT_SCREENSHOT");
  if (string.IsNullOrEmpty(output)) return 0;
  var device = Main.instance.GraphicsDevice; var oldTargets = device.GetRenderTargets();
  using var target = new RenderTarget2D(device, Main.screenWidth, Main.screenHeight);
  using var batch = new SpriteBatch(device);
  SpriteBatch oldBatch = Main.spriteBatch; bool menu = Main.gameMenu, inventory = Main.playerInventory, left = Main.mouseLeft, release = Main.mouseLeftRelease;
  Item mouse = Main.mouseItem; int mx = Main.mouseX, my = Main.mouseY, bottom = Main.instance.invBottom; Color oldBackground = Main.inventoryBack;
  AutoUncraftPlayer player = Main.LocalPlayer.GetModPlayer<AutoUncraftPlayer>();
  try {
   Main.gameMenu = false; Main.playerInventory = true; Main.mouseLeft = false; Main.mouseLeftRelease = false;
   Main.spriteBatch = batch; Main.inventoryBack = Color.White; Main.instance.invBottom = 210; Main.mouseX = Main.mouseY = -100;
   player.Enabled = false; player.Filters.Clear(); player.Input[0] = new Item(); AutoTrashSlot.SetItem(Main.LocalPlayer, new Item()); Main.mouseItem = new Item(ItemID.WoodenChair, 2); showList = false;
   device.SetRenderTarget(target); device.Clear(new Color(14, 20, 35));
   batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Main.UIScale));
   Rectangle slot = SlotBounds((int)(Main.screenWidth / Main.UIScale), (int)(Main.screenHeight / Main.UIScale), 210, false, autoX(), autoY());
   Main.mouseX = (int)(slot.Center.X * Main.UIScale); Main.mouseY = (int)(slot.Center.Y * Main.UIScale);
   Main.mouseLeft = Main.mouseLeftRelease = true; Draw();
   if (player.Filters.Count != 1 || !Main.mouseItem.IsAir || player.Input[0].stack != 2) throw new InvalidOperationException("Actual slot click must move the stack off the cursor.");
   Main.mouseLeftRelease = true; Draw();
   if (Main.mouseItem.stack != 2 || !player.Input[0].IsAir) throw new InvalidOperationException("Actual slot click must return the stack to the cursor.");
   Main.mouseLeftRelease = true; Draw();
   if (!Main.mouseItem.IsAir || player.Input[0].stack != 2 || player.Filters.Count != 1) throw new InvalidOperationException("Redeposit must preserve the rule and stack.");
   Main.mouseX = (int)((slot.Right + 12) * Main.UIScale); Main.mouseY = (int)((slot.Top + 10) * Main.UIScale);
   Main.mouseLeftRelease = true; Draw();
   if (!player.Enabled) throw new InvalidOperationException("Actual slot switch did not enable processing.");
   Main.mouseY = (int)((slot.Top + 31) * Main.UIScale); Main.mouseLeftRelease = true; Draw();
   if (!showList) throw new InvalidOperationException("Actual list button did not open the editor.");
   Main.mouseItem = new Item(ItemID.DirtBlock, 4);
   Main.mouseX = (int)((slot.X + 92) * Main.UIScale); Main.mouseY = (int)(slot.Center.Y * Main.UIScale);
   Main.mouseLeftRelease = true; Draw();
   if (!Main.mouseItem.IsAir || AutoTrashSlot.Item(Main.LocalPlayer).stack != 4 || !AutoTrashSlot.Filters(Main.LocalPlayer).Any(i => i.type == ItemID.DirtBlock))
    throw new InvalidOperationException("Auto Trash must accept the actual cursor stack and its rule.");
   Main.mouseLeftRelease = true; Draw();
   if (Main.mouseItem.stack != 4 || !AutoTrashSlot.Item(Main.LocalPlayer).IsAir || AutoTrashSlot.Filters(Main.LocalPlayer).Any(i => i.type == ItemID.DirtBlock))
    throw new InvalidOperationException("Auto Trash must return its actual recovery stack.");
   Main.mouseItem = new Item(ItemID.DirtBlock, 4); Main.mouseLeftRelease = true; Draw();
   Main.mouseX = (int)((slot.Right + 84) * Main.UIScale); Main.mouseY = (int)((slot.Top + 31) * Main.UIScale);
   Main.mouseLeftRelease = true; Draw();
   if (!showList || !trashList) throw new InvalidOperationException("Auto Trash needs its own editable list.");
   // Save the render with the uncraft editor open; both real slots are populated.
   trashList = false;
   Main.mouseLeft = Main.mouseLeftRelease = false; Main.mouseX = Main.mouseY = -100;
   batch.End(); device.Clear(new Color(14, 20, 35));
   batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Main.UIScale));
   Draw(); batch.End(); device.SetRenderTargets(oldTargets);
   using var file = System.IO.File.Create(output); target.SaveAsPng(file, target.Width, target.Height);
   return 8;
  } finally {
   device.SetRenderTargets(oldTargets); Main.spriteBatch = oldBatch; Main.gameMenu = menu; Main.playerInventory = inventory;
   Main.mouseLeft = left; Main.mouseLeftRelease = release; Main.mouseItem = mouse; Main.mouseX = mx; Main.mouseY = my; Main.instance.invBottom = bottom; Main.inventoryBack = oldBackground;
   showList = false;
  }
 }
 public override void Unload() { showList = false; page = 0; autoX = autoY = null; }
}
