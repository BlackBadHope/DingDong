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
	internal static void RequestQuestInteraction(int npcIndex, bool requestEscort)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			NpcQuestSystem.HandleInteraction(Main.LocalPlayer, npcIndex, requestEscort);
			return;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.QuestInteractRequest);
		WriteQuestInteractPayload(packet, npcIndex, requestEscort);
		packet.Send();
	}

	internal static void RequestQuestChoice(int npcIndex, int choice)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			NpcQuestSystem.HandleChoice(Main.LocalPlayer, npcIndex, choice);
			return;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.QuestChoiceRequest);
		packet.Write((short)npcIndex); packet.Write((byte)Math.Clamp(choice, 0, 255));
		packet.Send();
	}

	// Abandon needs no proximity: the performer may give up from anywhere in the book.
	internal static void RequestQuestAbandon(string npcKey)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			NpcQuestSystem.HandleAbandon(Main.LocalPlayer, npcKey);
			return;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient || !NpcTownManifest.TryGet(npcKey, out _))
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.QuestAbandonRequest);
		packet.Write(npcKey);
		packet.Send();
	}

	internal static void RequestFlagClaim(int tileX, int tileY)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			NpcResidenceWorldSystem.TryClaimFlag(Main.LocalPlayer, new Point16(tileX, tileY));
			return;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.FlagClaimRequest);
		WriteFlagClaimPayload(packet, tileX, tileY);
		packet.Send();
	}
}
