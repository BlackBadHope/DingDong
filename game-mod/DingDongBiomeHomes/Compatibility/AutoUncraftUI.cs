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
  int x = (chest ? 453 : 448) - (int)(56 * scale) + (int)((ox - 8) * 56 * scale) - 72;
  int y = bottom + (chest ? 168 : 0) + (int)((oy - 5) * 56 * scale) - 2;
  return new Rectangle(Math.Clamp(x, 4, Math.Max(4, width - 72)), Math.Clamp(y, 4, Math.Max(4, height - 56)), 40, 40);
 }
 private static bool Draw() {
  if (!Main.playerInventory || Main.gameMenu || PlayerInput.IgnoreMouseInterface || !UncraftRecipeCompatibility.Installed) { showList = false; return true; }
  SpriteBatch batch = Main.spriteBatch;
  int width = (int)(Main.screenWidth / Main.UIScale), height = (int)(Main.screenHeight / Main.UIScale);
  Rectangle slot = SlotBounds(width, height, Main.instance.invBottom, (Main.LocalPlayer.chest != -1 && !Main.recBigList) || Main.npcShop > 0, autoX?.Invoke() ?? 8, autoY?.Invoke() ?? 5);
  AutoUncraftPlayer player = Main.LocalPlayer.GetModPlayer<AutoUncraftPlayer>();
  Item preview = !Main.mouseItem.IsAir ? Main.mouseItem : player.LastItem;
  DrawSlotBackground(batch, slot);
  float old = Main.inventoryScale;
  try { Main.inventoryScale = 40f / TextureAssets.InventoryBack.Value.Width; ItemSlot.Draw(batch, new[] { preview }, ItemSlot.Context.BankItem, 0, slot.TopLeft()); }
  finally { Main.inventoryScale = old; }
  if (preview.IsAir) Utils.DrawBorderString(batch, "U", slot.TopLeft() + new Vector2(12, 8), Color.LightCyan, .8f);
  if (Hovered(slot)) {
   Main.hoverItemName = Text("SlotHint") + (!Main.mouseItem.IsAir ? "\n" + Preview(Main.mouseItem) : "");
   if (LeftClick()) { if (!Main.mouseItem.IsAir) player.Toggle(Main.mouseItem); else showList = !showList; Click(); }
   if (Main.mouseRight && Main.mouseRightRelease && !Main.mouseItem.IsAir) {
    if (UncraftRecipeCompatibility.TryBatch(Main.mouseItem, out string reason)) Recipe.FindRecipes();
    else Main.NewText(string.IsNullOrEmpty(reason) ? Text("CannotUncraft") : reason);
    Main.mouseRightRelease = false;
   }
  }
  Rectangle enabled = new(slot.Right + 3, slot.Top, 22, 20), list = new(slot.Right + 3, slot.Top + 21, 22, 20);
  batch.Draw((player.Enabled ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff).Value, enabled, Color.White);
  batch.Draw(TextureAssets.InfoIcon[5].Value, list, Color.White);
  if (Hovered(enabled)) { Main.hoverItemName = Text(player.Enabled ? "Enabled" : "Disabled", player.Filters.Count); if (LeftClick()) { player.Enabled = !player.Enabled; Click(); } }
  if (Hovered(list)) { Main.hoverItemName = Text("ShowList"); if (LeftClick()) { showList = !showList; Click(); } }
  if (showList) DrawList(batch, player, slot, width, height);
  return true;
 }
 private static string Preview(Item item) {
  Recipe recipe = UncraftRecipeCompatibility.BasicRecipe(item);
  if (recipe == null) return Text("CannotUncraft");
  return Text("Preview", recipe.createItem.stack, item.Name, string.Join(", ", recipe.requiredItem.Select(i => i.Name + " x" + i.stack))) + "\n" + Text("RatioHint");
 }
 private static void DrawList(SpriteBatch batch, AutoUncraftPlayer player, Rectangle slot, int width, int height) {
  int pw = Math.Min(460, width - 8), ph = Math.Min(330, height - 8);
  Rectangle panel = new(Math.Clamp(slot.X - 280, 4, Math.Max(4, width - pw - 4)), Math.Clamp(slot.Bottom + 16, 4, Math.Max(4, height - ph - 4)), pw, ph);
  batch.Draw(TextureAssets.MagicPixel.Value, panel, new Color(23, 38, 68, 245)); Hovered(panel);
  Utils.DrawBorderString(batch, Text("Title", player.Filters.Count), panel.TopLeft() + new Vector2(12, 10), Color.LightCyan, .85f);
  Utils.DrawBorderString(batch, Text("EditHint"), panel.TopLeft() + new Vector2(12, 40), Color.White, .66f);
  Utils.DrawBorderString(batch, Text("SafetyHint"), panel.TopLeft() + new Vector2(12, 61), Color.Silver, .63f);
  Button(batch, new Rectangle(panel.Right - 29, panel.Top + 6, 22, 24), "X", () => showList = false);
  int pages = Math.Max(1, (player.Filters.Count + 39) / 40); page = Math.Clamp(page, 0, pages - 1);
  float old = Main.inventoryScale;
  try {
   Main.inventoryScale = .68f;
   for (int n = page * 40; n < Math.Min(player.Filters.Count, (page + 1) * 40); n++) {
    int i = n - page * 40; Rectangle box = new(panel.X + 12 + i % 10 * 43, panel.Y + 94 + i / 10 * 44, 38, 38);
    Item item = player.Filters[n]; DrawSlotBackground(batch, box); ItemSlot.Draw(batch, new[] { item }, ItemSlot.Context.BankItem, 0, box.TopLeft());
    if (Hovered(box)) { Main.hoverItemName = item.Name + "\n" + Preview(item) + "\n" + Text("RemoveHint"); if (LeftClick()) { player.Filters.RemoveAt(n); Click(); break; } }
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
   if (slot.X < 0 || slot.Right + 25 > width || slot.Bottom > 720) throw new InvalidOperationException("Auto-uncraft slot outside viewport.");
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
   player.Enabled = false; player.Filters.Clear(); Main.mouseItem = new Item(ItemID.WoodenChair, 2); showList = false;
   device.SetRenderTarget(target); device.Clear(new Color(14, 20, 35));
   batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Main.UIScale));
   Rectangle slot = SlotBounds((int)(Main.screenWidth / Main.UIScale), (int)(Main.screenHeight / Main.UIScale), 210, false, autoX(), autoY());
   Main.mouseX = (int)(slot.Center.X * Main.UIScale); Main.mouseY = (int)(slot.Center.Y * Main.UIScale);
   Main.mouseLeft = Main.mouseLeftRelease = true; Draw();
   if (player.Filters.Count != 1 || Main.mouseItem.stack != 2) throw new InvalidOperationException("Actual slot click did not register an unchanged sample.");
   Main.mouseX = (int)((slot.Right + 12) * Main.UIScale); Main.mouseY = (int)((slot.Top + 10) * Main.UIScale);
   Main.mouseLeftRelease = true; Draw();
   if (!player.Enabled) throw new InvalidOperationException("Actual slot switch did not enable processing.");
   Main.mouseY = (int)((slot.Top + 31) * Main.UIScale); Main.mouseLeftRelease = true; Draw();
   if (!showList) throw new InvalidOperationException("Actual list button did not open the editor.");
   Main.mouseLeft = Main.mouseLeftRelease = false; Main.mouseX = Main.mouseY = -100;
   batch.End(); device.Clear(new Color(14, 20, 35));
   batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Main.UIScale));
   Mod auto = ModLoader.GetMod("AutoTrash"); Type native = auto.Code.GetType("AutoTrash.AutoTrashGlobalItem", true);
   MethodInfo draw = native.GetMethod("DrawUpdateAutoTrash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
    ?? throw new MissingMethodException(native.FullName, "DrawUpdateAutoTrash");
   draw.Invoke(draw.IsStatic ? null : auto.GetContent<GlobalItem>().Single(g => g.GetType() == native), null);
   Draw(); batch.End(); device.SetRenderTargets(oldTargets);
   using var file = System.IO.File.Create(output); target.SaveAsPng(file, target.Width, target.Height);
   return 3;
  } finally {
   device.SetRenderTargets(oldTargets); Main.spriteBatch = oldBatch; Main.gameMenu = menu; Main.playerInventory = inventory;
   Main.mouseLeft = left; Main.mouseLeftRelease = release; Main.mouseItem = mouse; Main.mouseX = mx; Main.mouseY = my; Main.instance.invBottom = bottom; Main.inventoryBack = oldBackground;
   showList = false;
  }
 }
 public override void Unload() { showList = false; page = 0; autoX = autoY = null; }
}
