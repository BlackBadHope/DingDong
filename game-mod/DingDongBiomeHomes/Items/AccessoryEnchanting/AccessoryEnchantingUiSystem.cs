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
public sealed class AccessoryEnchantingUiSystem : ModSystem
{
	private static UserInterface userInterface;
	private static AccessoryEnchantingUiState state;
	private static GameTime lastGameTime = new();
	internal static bool Visible
	{
		get
		{
			Player player = Main.LocalPlayer;
			bool talkingToGoblin = player != null && player.talkNPC >= 0 && player.talkNPC < Main.maxNPCs && Main.npc[player.talkNPC].active && Main.npc[player.talkNPC].type == NPCID.GoblinTinkerer;
			return ShouldShowForState(Main.dedServ, Main.gameMenu, Main.playerInventory, Main.InReforgeMenu, talkingToGoblin);
		}
	}

	internal static bool ShouldShowForState(bool dedicatedServer, bool gameMenu, bool playerInventory, bool inReforgeMenu, bool talkingToGoblin) => !dedicatedServer && !gameMenu && playerInventory && inReforgeMenu && talkingToGoblin;
	internal static void NotifyResult(bool success, string failure)
	{
		if (state == null)
			return;
		state.NotifyResult(success, failure);
	}

	public override void Load()
	{
		if (Main.dedServ)
			return;
		On_Main.DrawInventory += DrawPendingInventory;
		state = new AccessoryEnchantingUiState();
		state.Activate();
		userInterface = new UserInterface();
		userInterface.SetState(state);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		lastGameTime = gameTime;
		if (Main.netMode == NetmodeID.MultiplayerClient && !Main.gameMenu)
		{
			AccessoryStationSession session = Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session;
			if (Netplay.Disconnect)
				session.Reset();
			else if (session.TimedOut((uint)Main.GameUpdateCount))
				AccessoryEnchantingNetwork.AbortPending("server response timed out");
		}
		if (Visible)
			userInterface?.Update(gameTime);
	}

	private static void DrawPendingInventory(On_Main.orig_DrawInventory orig, Main self)
	{
		bool pending = Main.netMode == NetmodeID.MultiplayerClient && !Main.gameMenu && Main.LocalPlayer.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.InventoryLocked;
		if (!pending)
		{
			orig(self);
			return;
		}
		// Only inventory interactions wait for the reply. World, movement and
		// remote AI continue normally, and the menu remains available to exit.
		bool left = Main.mouseLeft, right = Main.mouseRight;
		bool leftRelease = Main.mouseLeftRelease, rightRelease = Main.mouseRightRelease;
		Main.mouseLeft = Main.mouseRight = Main.mouseLeftRelease = Main.mouseRightRelease = false;
		try { orig(self); }
		finally
		{
			Main.mouseLeft = left;
			Main.mouseRight = right;
			Main.mouseLeftRelease = leftRelease;
			Main.mouseRightRelease = rightRelease;
		}
	}

	public override void OnWorldUnload()
	{
		if (!Main.dedServ)
			Main.LocalPlayer?.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.Reset();
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		if (!Visible || userInterface == null)
			return;
		int inventoryIndex = layers.FindIndex(layer => string.Equals(layer.Name, "Vanilla: Inventory", StringComparison.Ordinal));
		if (inventoryIndex < 0)
			inventoryIndex = layers.Count;
		layers.Insert(inventoryIndex + (inventoryIndex < layers.Count ? 1 : 0), new LegacyGameInterfaceLayer("DingDongBiomeHomes: Goblin Accessory Fusion", () =>
		{
			userInterface.Draw(Main.spriteBatch, lastGameTime);
			return true;
		}, InterfaceScaleType.UI));
	}

	public override void Unload()
	{
		if (!Main.dedServ)
			On_Main.DrawInventory -= DrawPendingInventory;
		userInterface = null;
		state = null;
		lastGameTime = new GameTime();
	}
}
