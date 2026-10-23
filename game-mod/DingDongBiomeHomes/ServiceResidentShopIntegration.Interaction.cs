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
using Terraria.ModLoader.UI;

namespace DingDongBiomeHomes;
internal static partial class ServiceResidentShopIntegration
{
	private const int InventoryRepeatDelayTicks = 15;

	internal static void ArmInventoryRepeatDelay() =>
		Main.stackSplit = Math.Min(Main.stackSplit, InventoryRepeatDelayTicks);

	private static void ActivateBubble(NPC npc, DingDongDialogueBubbleAction action)
	{
		switch (action)
		{
			case DingDongDialogueBubbleAction.Quest:
				NpcQuestSystem.RequestDialogueInteraction(npc, Main.LocalPlayer, requestEscort: false);
				break;
			case DingDongDialogueBubbleAction.Escort:
				NpcQuestSystem.RequestDialogueInteraction(npc, Main.LocalPlayer, requestEscort: true);
				break;
			case DingDongDialogueBubbleAction.HomeGoods:
				ServiceResidentShopGlobalNPC.OpenHomeGoods(npc);
				break;
			case DingDongDialogueBubbleAction.GoblinFusion:
				Main.playerInventory = true;
				ArmInventoryRepeatDelay();
				Main.npcChatText = string.Empty;
				Main.SetNPCShopIndex(0);
				Main.InReforgeMenu = true;
				SoundEngine.PlaySound(SoundID.MenuOpen);
				break;
		}
	}
}
