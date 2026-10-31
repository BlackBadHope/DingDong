using Terraria;
using Terraria.ID;
using DingDongBiomeHomes.Items.AccessoryEnchanting;

namespace DingDongBiomeHomes;

// A station reply commits the cursor captured when its request began. Custom UI and
// automatic processors must leave local inventory/cursor state alone until that commit.
internal static class InventoryTransactionGuard
{
	internal static bool IsBlocked(Player player)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return false;
		if (player == null) return true;
		if (player.whoAmI != Main.myPlayer) return false;
		// AbortPending resets the session before disconnecting. Keep mutations blocked
		// during that uncertain connection too; a reconnect creates a fresh session.
		return Netplay.Disconnect || player.GetModPlayer<AccessoryEnchantingStationPlayer>().Session.InventoryLocked;
	}
}
