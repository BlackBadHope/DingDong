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
	internal static void SendPlayerIdentity(string persistentId)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.PlayerIdentityHello);
		WritePlayerIdentityPayload(packet, persistentId);
		packet.Send();
	}

	internal static void SendQuestSnapshot(Player player, int toClient)
    {
        if (Main.netMode != NetmodeID.Server) return;
        foreach (Player recipient in Main.ActivePlayers)
            if (recipient.GetModPlayer<NpcQuestPlayer>().IdentityConfirmed) SendSnapshotTo(recipient, recipient.whoAmI);
    }
	private static void SendSnapshotTo(Player player, int toClient)
	{
		if (Main.netMode != NetmodeID.Server || !CanSendToClient(toClient))
			return;
		IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> records = player.GetModPlayer<NpcQuestPlayer>().CurrentSnapshot();
		ModPacket packet = DingDongBiomeHomes.Instance.GetPacket();
		packet.Write((byte)DingDongPacketKind.QuestSnapshot);
		packet.Write(QuestProtocolVersion);
		packet.Write(QuestContentRegistry.CatalogFingerprint);
		packet.Write(player.GetModPlayer<NpcQuestPlayer>().Revision);
		packet.Write((byte)player.whoAmI);
		packet.Write((ushort)records.Count);
		foreach ((string npcKey, ResidentQuestProgress progress)in records)
		{
			packet.Write(npcKey);
			packet.Write(progress.Completed);
			packet.Write(progress.Active);
			packet.Write(progress.Progress);
			packet.Write(progress.ActiveQuestId);
			packet.Write((byte)progress.ActiveKind);
			packet.Write(progress.ActiveVariant);
			packet.Write(progress.ActiveItemType);
			packet.Write(progress.ActiveGoal);
			packet.Write(progress.ActiveRadiusTiles);
			packet.Write(progress.ActiveTargetKey ?? string.Empty);
			packet.Write(progress.ActiveTargetName ?? string.Empty);
			packet.Write(progress.RewardCopper);
			packet.Write((byte)progress.DifficultyRank);
			packet.Write(progress.ActiveStoryKey); packet.Write(progress.RewardItemType); packet.Write(progress.RewardItemCount);
			packet.Write(progress.AssignedTo); packet.Write(progress.AssignedName); packet.Write(progress.LastCompletedBy);
			packet.Write(progress.Paused); packet.Write(progress.StartClock); packet.Write(progress.Baseline);
		}

		WriteSharedHistory(packet);
		WriteWorldState(packet);
		packet.Send(toClient);
	}

	internal static bool CanSendToClient(int toClient) => Main.netMode == NetmodeID.Server && toClient >= 0 && toClient < Main.maxPlayers && Netplay.Clients[toClient].IsActive;
}
