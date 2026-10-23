using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed class AccessoryStationSlotElement : UIElement
{
	private readonly AccessoryStationSlot slot;
	private readonly Action<string, bool> status;
	internal AccessoryStationSlotElement(AccessoryStationSlot slot, Action<string, bool> status)
	{
		this.slot = slot;
		this.status = status;
		OnLeftClick += SlotClicked;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!ContainsPoint(Main.MouseScreen))
			return;
		Main.LocalPlayer.mouseInterface = true;
		ref Item item = ref CurrentItem();
		if (!item.IsAir)
			ItemSlot.MouseHover(ref item, ItemSlot.Context.BankItem);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		CalculatedStyle dimensions = GetDimensions();
		ref Item item = ref CurrentItem();
		ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.BankItem, new Vector2(dimensions.X, dimensions.Y), Color.White);
	}

	private void SlotClicked(UIMouseEvent evt, UIElement listeningElement)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient && !Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.Ready)
			return;
		if (!AccessoryEnchantingStationPlayer.Accepts(Main.mouseItem))
		{
			status(Word("Accessories only", "Только аксессуары"), false);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			AccessoryEnchantingNetwork.RequestSlotSwap(Main.LocalPlayer, slot);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		Item mouse = Main.mouseItem;
		AccessoryEnchantingStationPlayer station = Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>();
		if (!station.TrySwap(slot, ref mouse, station.Revision, AccessoryEnchantingNetwork.TalkingToGoblin(Main.LocalPlayer), out string failure))
		{
			status(failure, false);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		Main.mouseItem = mouse;
		Main.mouseItem.newAndShiny = false;
		Recipe.FindRecipes();
		SoundEngine.PlaySound(SoundID.Grab);
	}

	internal static bool TrySwap(ref Item stationItem, ref Item mouseItem, out string failure)
	{
		if (!AccessoryEnchantingStationPlayer.Accepts(mouseItem))
		{
			failure = Word("Accessories only", "Только аксессуары");
			return false;
		}

		Item swap = stationItem;
		stationItem = mouseItem;
		mouseItem = swap;
		failure = string.Empty;
		return true;
	}

	private ref Item CurrentItem() => ref Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Slot(slot);
	private static string Word(string english, string russian) => Language.ActiveCulture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true ? russian : english;
}
