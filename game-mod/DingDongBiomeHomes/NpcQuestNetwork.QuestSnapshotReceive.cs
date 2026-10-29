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
	private static void ReceiveQuestSnapshot(BinaryReader reader)
	{
		byte protocolVersion = reader.ReadByte();
		ulong catalogFingerprint = reader.ReadUInt64();
		uint revision = reader.ReadUInt32();
		if (protocolVersion != QuestProtocolVersion)
			throw new InvalidDataException($"Unsupported quest protocol {protocolVersion}.");
		if (catalogFingerprint != QuestContentRegistry.CatalogFingerprint)
			throw new InvalidDataException("Quest catalog fingerprint mismatch.");
		int playerIndex = reader.ReadByte();
		int count = reader.ReadUInt16();
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || count > NpcTownManifest.AuthoredCount)
			throw new InvalidDataException("Invalid quest snapshot header.");
		List<(string, ResidentQuestProgress)> records = new(count);
		for (int index = 0; index < count; index++)
		{
			string npcKey = reader.ReadString();
			if (npcKey.Length == 0 || npcKey.Length > MaximumQuestStringLength || !NpcTownManifest.TryGet(npcKey, out _) || records.Any(record => string.Equals(record.Item1, npcKey, StringComparison.Ordinal)))
			{
				throw new InvalidDataException("Invalid or duplicate quest resident key.");
			}

			ResidentQuestProgress progress = new()
			{
				Completed = Math.Max(0, reader.ReadInt32()),
				Active = reader.ReadBoolean(),
				Progress = Math.Max(0, reader.ReadInt32()),
				ActiveQuestId = reader.ReadInt32(),
				ActiveKind = (ResidentQuestKind)reader.ReadByte(),
				ActiveVariant = reader.ReadInt32(),
				ActiveItemType = reader.ReadInt32(),
				ActiveGoal = Math.Max(0, reader.ReadInt32()),
				ActiveRadiusTiles = Math.Max(0, reader.ReadInt32()),
				ActiveTargetKey = reader.ReadString(),
				ActiveTargetName = reader.ReadString(),
				RewardCopper = Math.Max(0, reader.ReadInt32()),
				DifficultyRank = reader.ReadByte(),
				ActiveStoryKey = reader.ReadString(), RewardItemType = reader.ReadInt32(), RewardItemCount = reader.ReadInt32(),
                AssignedTo = reader.ReadString(), AssignedName = reader.ReadString(), LastCompletedBy = reader.ReadString(),
				Paused = reader.ReadBoolean(), StartClock = reader.ReadInt64(), Baseline = Math.Max(0, reader.ReadInt32())
			};
			if (progress.ActiveStoryKey.Length > MaximumQuestStringLength || (progress.Active && progress.ActiveStoryKey.Length > 0 && (!PersonalResidentQuestCatalog.TryResolve(progress.ActiveStoryKey,out var authored) || authored.Id != progress.ActiveQuestId || authored.RewardItemType != progress.RewardItemType || authored.RewardItemCount != progress.RewardItemCount)) || progress.AssignedTo.Length > 32 || progress.AssignedName.Length > 60 || progress.LastCompletedBy.Length > 60 || progress.ActiveTargetKey.Length > MaximumQuestStringLength || progress.ActiveTargetName.Length > MaximumQuestStringLength || progress.ActiveQuestId < -1 || progress.ActiveQuestId > 1_000_000 || (progress.Active && (progress.ActiveQuestId < 0 || progress.ActiveGoal <= 0 || progress.Progress > progress.ActiveGoal)) || (!progress.Active && progress.ActiveQuestId != -1) || !Enum.IsDefined(progress.ActiveKind) || progress.DifficultyRank > 3 || (progress.Active && progress.ActiveKind is (ResidentQuestKind.DeliverSupplies or ResidentQuestKind.BannerCollection) && (progress.ActiveItemType <= ItemID.None || progress.ActiveItemType >= ItemLoader.ItemCount)) || (progress.Active && progress.ActiveKind is (ResidentQuestKind.BossHunt or ResidentQuestKind.BiomePurge or ResidentQuestKind.BannerCollection or ResidentQuestKind.ModProgression) && string.IsNullOrWhiteSpace(progress.ActiveTargetKey)))
			{
				throw new InvalidDataException("Invalid active quest id.");
			}

			// Authored target names are words: resolve them in this client's language.
			if (progress.Active && progress.ActiveStoryKey.Length > 0 && PersonalResidentQuestCatalog.TryResolve(progress.ActiveStoryKey, out var local))
				progress.ActiveTargetName = local.TargetName;
			records.Add((npcKey, progress));
		}

		var history = ReadSharedHistory(reader);
		ReadWorldState(reader, out long clock, out var choices);
		NpcQuestPlayer questPlayer = Main.player[playerIndex].GetModPlayer<NpcQuestPlayer>();
        if (revision >= questPlayer.Revision) { NpcQuestWorldLedger.ReplaceNetworkHistory(history); NpcQuestWorldLedger.ReplaceNetworkWorldState(clock, choices); }
		questPlayer.ReplaceCurrentNetworkSnapshot(records, revision);
		if (playerIndex == Main.myPlayer)
			questPlayer.ConfirmNetworkIdentity();
	}
}
