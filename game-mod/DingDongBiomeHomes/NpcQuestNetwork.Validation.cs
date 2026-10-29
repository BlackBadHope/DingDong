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
	internal static bool ContractPass(out string failure)
	{
		failure = string.Empty;
		if (QuestContentRegistry.CatalogFingerprint == 0)
			failure = "catalog-fingerprint";
		else if (ResidentQuestCatalog.PoolSize <= ResidentQuestCatalog.BasePoolSize)
			failure = "dynamic-catalog-empty";
		else if (!PlayerIdentityPacketContractPass())
			failure = "identity-packet";
		else if (!QuestInteractPacketContractPass())
			failure = "interact-packet";
		else if (!FlagClaimPacketContractPass())
			failure = "flag-claim-packet";
		else if (!WorldStatePacketContractPass())
			failure = "world-state-packet";
		else if (!QuestNotice.PacketContractPass())
			failure = "notice-packet";
		return failure.Length == 0;
	}

	internal static void HandleQuestInteractionForProbe(int sender, int npcIndex, bool requestEscort)
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			{ var p = NpcResidentRegistry.TryGet(Main.npc[npcIndex],out string key,out _) ? Main.player[sender].GetModPlayer<NpcQuestPlayer>().Get(key) : null;
			WriteCheckedQuestInteraction(writer,npcIndex,requestEscort,p?.ActiveQuestId ?? -1,p?.Completed ?? 0); }
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		HandlePacket(DingDongPacketKind.QuestInteractRequest, reader, sender);
	}
}
