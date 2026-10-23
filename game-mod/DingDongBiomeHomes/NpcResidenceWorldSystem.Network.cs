using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class NpcResidenceWorldSystem
{
	internal static void ReceiveSnapshot(BinaryReader reader) => ReadSnapshot(reader);
	internal static void SendSnapshot(int toWho)
	{
		if (Main.netMode != NetmodeID.Server || (toWho >= 0 && !NpcQuestNetwork.CanSendToClient(toWho)) || (toWho < 0 && !NpcQuestNetwork.HasConnectedClients()))
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.EscortSnapshot);
		WriteSnapshot(packet);
		packet.Send(toWho);
	}

	private static void SyncFlag(ResidenceFlagTileEntity flag)
	{
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, flag.ID, flag.Position.X, flag.Position.Y);
	}
}
