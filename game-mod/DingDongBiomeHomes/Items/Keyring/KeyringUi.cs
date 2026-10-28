using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.Keyring;

/// <summary>Compact grid of the stored keys; click a key to take one stack out.</summary>
public sealed class KeyringUi : ModSystem
{
	private static KeyringItem open;
	internal static void Toggle(KeyringItem ring)
	{
		open = ReferenceEquals(open, ring) ? null : ring;
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (index >= 0) layers.Insert(index, new LegacyGameInterfaceLayer("DingDong: Keyring", Draw, InterfaceScaleType.UI));
	}
	private static bool Draw()
	{
		if (open == null) return true;
		if (!Main.playerInventory || Main.gameMenu || KeyringItem.Find(Main.LocalPlayer) == null) { open = null; return true; }
		KeyringItem ring = KeyringItem.Find(Main.LocalPlayer);
		if (!ReferenceEquals(ring, open)) open = ring;
		SpriteBatch batch = Main.spriteBatch;
		var groups = open.Stored.GroupBy(i => i.type).Select(g => (Type: g.Key, Stack: g.Sum(i => i.stack))).ToList();
		int columns = 8, rows = Math.Max(1, (groups.Count + columns - 1) / columns);
		int width = 12 + columns * 44 + 12, height = 46 + rows * 44 + 12;
		Rectangle panel = new(Math.Clamp(Main.screenWidth / 2 - width / 2, 4, Math.Max(4, Main.screenWidth - width - 4)), 140, width, height);
		batch.Draw(TextureAssets.MagicPixel.Value, panel, new Color(23, 38, 68, 245));
		if (panel.Contains(Main.mouseX, Main.mouseY)) { Main.LocalPlayer.mouseInterface = true; Main.LocalPlayer.cursorItemIconEnabled = false; }
		Utils.DrawBorderString(batch, Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.Title", groups.Count, open.Stored.Sum(i => i.stack)), panel.TopLeft() + new Vector2(12, 10), Color.LightCyan, .85f);
		Rectangle close = new(panel.Right - 30, panel.Top + 8, 22, 22);
		batch.Draw(TextureAssets.MagicPixel.Value, close, close.Contains(Main.mouseX, Main.mouseY) ? new Color(120, 60, 60) : new Color(70, 40, 40));
		Utils.DrawBorderString(batch, "X", close.TopLeft() + new Vector2(6, 1), Color.White, .8f);
		if (close.Contains(Main.mouseX, Main.mouseY) && Main.mouseLeft && Main.mouseLeftRelease) { Main.mouseLeftRelease = false; open = null; SoundEngine.PlaySound(SoundID.MenuClose); return true; }
		float old = Main.inventoryScale;
		try
		{
			Main.inventoryScale = .7f;
			for (int n = 0; n < groups.Count; n++)
			{
				Rectangle box = new(panel.X + 12 + n % columns * 44, panel.Y + 46 + n / columns * 44, 40, 40);
				Item shown = new(groups[n].Type); shown.stack = groups[n].Stack;
				batch.Draw(TextureAssets.MagicPixel.Value, box, new Color(91, 120, 155));
				ItemSlot.Draw(batch, new[] { shown }, ItemSlot.Context.BankItem, 0, box.TopLeft());
				if (!box.Contains(Main.mouseX, Main.mouseY)) continue;
				Main.LocalPlayer.mouseInterface = true;
				Main.HoverItem = shown.Clone();
				Main.hoverItemName = shown.Name + "\n" + Language.GetTextValue("Mods.DingDongBiomeHomes.Keyring.TakeHint");
				if (Main.mouseLeft && Main.mouseLeftRelease && Main.mouseItem.IsAir)
				{
					Main.mouseLeftRelease = false;
					int count = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ? shown.maxStack : 1;
					Item taken = open.Take(groups[n].Type, count);
					if (!taken.IsAir) { Main.mouseItem = taken; SoundEngine.PlaySound(SoundID.Grab); }
				}
			}
		}
		finally { Main.inventoryScale = old; }
		return true;
	}
	public override void Unload() => open = null;
}
