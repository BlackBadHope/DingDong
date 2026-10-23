using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class NpcQuestNetwork
{
	internal static void HandlePacket(DingDongPacketKind kind, BinaryReader reader, int whoAmI)
	{
		try
		{
			switch (kind)
			{
				case DingDongPacketKind.QuestInteractRequest when Main.netMode == NetmodeID.Server:
					ReadQuestInteractPayload(reader, out short npcIndex, out bool requestEscort);
					if (ValidPlayer(whoAmI))
						NpcQuestSystem.HandleInteraction(Main.player[whoAmI], npcIndex, requestEscort);
					break;
				case DingDongPacketKind.FlagClaimRequest when Main.netMode == NetmodeID.Server:
					ReadFlagClaimPayload(reader, out short tileX, out short tileY);
					if (ValidPlayer(whoAmI))
						NpcResidenceWorldSystem.TryClaimFlag(Main.player[whoAmI], new Point16(tileX, tileY));
					break;
				case DingDongPacketKind.PlayerIdentityHello when Main.netMode == NetmodeID.Server:
					string persistentId = ReadPlayerIdentityPayload(reader);
					if (ValidPlayer(whoAmI))
						ReceivePlayerIdentity(Main.player[whoAmI], persistentId);
					break;
				case DingDongPacketKind.QuestSnapshot when Main.netMode == NetmodeID.MultiplayerClient:
					ReceiveQuestSnapshot(reader);
					break;
				case DingDongPacketKind.EscortSnapshot when Main.netMode == NetmodeID.MultiplayerClient:
					NpcResidenceWorldSystem.ReceiveSnapshot(reader);
					break;
				default:
					throw new InvalidDataException($"Unexpected DingDong packet kind {(byte)kind} for netMode={Main.netMode}.");
			}
		}
		catch (Exception exception)when (exception is EndOfStreamException or IOException or InvalidDataException or ArgumentException or FormatException)
		{
			DingDongBiomeHomes.ConsumeRemainingPacket(reader);
			PacketRejectGuard.Reject("GAMEPLAY", whoAmI, (byte)kind, exception.GetType().Name);
		}
	}
}
