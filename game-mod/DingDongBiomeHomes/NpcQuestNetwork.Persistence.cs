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
        int questId = -1, completed = 0;
        if (npcIndex >= 0 && npcIndex < Main.maxNPCs && NpcResidentRegistry.TryGet(Main.npc[npcIndex], out string key, out _) && Main.LocalPlayer != null)
        {
            var p = Main.LocalPlayer.GetModPlayer<NpcQuestPlayer>().Get(key);
            questId = p.ActiveQuestId; completed = p.Completed;
        }
        WriteCheckedQuestInteraction(writer,npcIndex,requestEscort,questId,completed);
    }
    internal static void WriteCheckedQuestInteraction(BinaryWriter writer, int npcIndex, bool escort, int questId, int completed)
    {
        writer.Write(QuestProtocolVersion); writer.Write((short)npcIndex); writer.Write(escort);
        writer.Write(questId); writer.Write(completed);
    }
    private static void ReadQuestInteractPayload(BinaryReader reader, out short npcIndex, out bool requestEscort) =>
        ReadQuestInteractPayload(reader,out npcIndex,out requestEscort,out _,out _);
    private static void ReadQuestInteractPayload(BinaryReader reader, out short npcIndex, out bool requestEscort, out int questId, out int completed)
    {
        if(reader.ReadByte()!=QuestProtocolVersion) throw new InvalidDataException("Unsupported quest interaction protocol");
        npcIndex=reader.ReadInt16();requestEscort=reader.ReadBoolean();questId=reader.ReadInt32();completed=reader.ReadInt32();
        if(questId < -1 || questId>1_000_000 || completed<0) throw new InvalidDataException("Invalid quest interaction version");
    }
    internal static bool MatchesSharedInteraction(Player player,int npcIndex,int questId,int completed)
    {
        if(npcIndex<0||npcIndex>=Main.maxNPCs||!NpcResidentRegistry.TryGet(Main.npc[npcIndex],out string key,out _)) return false;
        var p=NpcQuestWorldLedger.GetShared(key);
        return p.ActiveQuestId==questId&&p.Completed==completed;
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
