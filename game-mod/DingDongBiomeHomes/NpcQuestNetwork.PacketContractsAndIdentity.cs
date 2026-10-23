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
	private static bool QuestInteractPacketContractPass()
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			WriteQuestInteractPayload(writer, 1234, true);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		ReadQuestInteractPayload(reader, out short npcIndex, out bool requestEscort);
		return npcIndex == 1234 && requestEscort && stream.Position == stream.Length;
	}

	private static bool FlagClaimPacketContractPass()
	{
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			WriteFlagClaimPayload(writer, 320, 180);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		ReadFlagClaimPayload(reader, out short tileX, out short tileY);
		return tileX == 320 && tileY == 180 && stream.Position == stream.Length;
	}

	private static bool PlayerIdentityPacketContractPass()
	{
		const string identity = "00112233445566778899aabbccddeeff";
		using MemoryStream stream = new();
		using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			WritePlayerIdentityPayload(writer, identity);
		stream.Position = 0;
		using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
		return string.Equals(ReadPlayerIdentityPayload(reader), identity, StringComparison.Ordinal) && stream.Position == stream.Length && stream.Length == 33;
	}

	private static void ReceivePlayerIdentity(Player player, string persistentId)
	{
		if (!IdentityAvailableForPlayer(player.whoAmI, persistentId))
			throw new InvalidDataException("Player identity is already active on another connection.");
		if (!QuestIdentityBindingSystem.TryBind(player.whoAmI, persistentId, out string bindFailure))
			throw new InvalidDataException(bindFailure);
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		questPlayer.SetNetworkIdentity(persistentId);
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		NpcResidenceWorldSystem.HandlePlayerConnected(player);
		SendQuestSnapshot(player, player.whoAmI);
		NpcResidenceWorldSystem.SendSnapshot(player.whoAmI);
	}
}
