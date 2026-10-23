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
	private static void WriteQuestInteractPayload(BinaryWriter writer, int npcIndex, bool requestEscort)
	{
		writer.Write((short)npcIndex);
		writer.Write(requestEscort);
	}

	private static void ReadQuestInteractPayload(BinaryReader reader, out short npcIndex, out bool requestEscort)
	{
		npcIndex = reader.ReadInt16();
		requestEscort = reader.ReadBoolean();
	}

	private static void WriteFlagClaimPayload(BinaryWriter writer, int tileX, int tileY)
	{
		writer.Write((short)tileX);
		writer.Write((short)tileY);
	}

	private static void ReadFlagClaimPayload(BinaryReader reader, out short tileX, out short tileY)
	{
		tileX = reader.ReadInt16();
		tileY = reader.ReadInt16();
	}

	private static void WritePlayerIdentityPayload(BinaryWriter writer, string persistentId)
	{
		if (!Guid.TryParseExact(persistentId, "N", out _))
			throw new InvalidDataException("Invalid player identity.");
		writer.Write(persistentId);
	}

	private static string ReadPlayerIdentityPayload(BinaryReader reader)
	{
		string persistentId = reader.ReadString();
		if (persistentId.Length != 32 || !Guid.TryParseExact(persistentId, "N", out _))
			throw new InvalidDataException("Invalid player identity.");
		return persistentId;
	}
}
