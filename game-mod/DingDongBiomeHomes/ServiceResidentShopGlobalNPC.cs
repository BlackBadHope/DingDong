using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class ServiceResidentShopGlobalNPC : GlobalNPC
{
	internal static void OpenHomeGoods(NPC npc)
	{
		if (npc == null || !ServiceResidentShopIntegration.IsServiceResident(npc.type))
			return;

		Main.playerInventory = true;
		ServiceResidentShopIntegration.ArmInventoryRepeatDelay();
		Main.npcChatText = "";
		Main.SetNPCShopIndex(1);
		Main.instance.shop[Main.npcShop].SetupShop(
			NPCShopDatabase.GetShopName(npc.type, ServiceResidentShopIntegration.ShopName),
			npc);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
