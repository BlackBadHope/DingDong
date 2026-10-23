using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal static partial class AccessoryEnchantingNetwork
{
	internal static void RequestAction(AccessoryStationAction action)
	{
		Player player = Main.LocalPlayer;
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			AccessoryEnchantingStationPlayer station = player.GetModPlayer<AccessoryEnchantingStationPlayer>();
			if (!station.Session.TryBegin(importing: false, (uint)Main.GameUpdateCount, out uint request))
				return;
			ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
			packet.Write(ActionRequestPacket);
			packet.Write((byte)action);
			packet.Write(request);
			packet.Write(station.Revision);
			packet.Send();
			return;
		}

		if (!TalkingToGoblin(player))
		{
			AccessoryEnchantingUiSystem.NotifyResult(false, "goblin-session-required");
			return;
		}
		Execute(player, action, out bool success, out string failure);
		AccessoryEnchantingUiSystem.NotifyResult(success, failure);
	}
}
