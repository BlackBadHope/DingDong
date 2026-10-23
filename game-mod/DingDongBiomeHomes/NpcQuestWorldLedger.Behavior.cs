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
public sealed partial class NpcQuestWorldLedger
{
	internal static void BindPlayer(Player player, bool allowLocalImport)
	{
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (!questPlayer.IdentityConfirmed || questPlayer.PersistentId.Length == 0)
			return;
		string prefix = questPlayer.PersistentId + Separator;
		List<(string NpcKey, ResidentQuestProgress Progress)> authoritative = Records.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(pair => (pair.Key[prefix.Length..], pair.Value.Clone())).ToList();
		for (int index = 0; index < authoritative.Count; index++)
		{
			(string npcKey, ResidentQuestProgress progress) = authoritative[index];
			if (NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity) && progress.NormalizeLoaded(identity, questPlayer.PersistentId))
			{
				Records[Compose(questPlayer.PersistentId, npcKey)] = progress.Clone();
			}
		}

		if (allowLocalImport && authoritative.Count == 0)
		{
			IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> imported = questPlayer.CurrentSnapshot();
			foreach ((string npcKey, ResidentQuestProgress progress)in imported)
			{
				if (NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
					progress.NormalizeLoaded(identity, questPlayer.PersistentId);
				Records[Compose(questPlayer.PersistentId, npcKey)] = progress.Clone();
			}

			questPlayer.ReplaceCurrentSnapshot(imported);
			return;
		}

		questPlayer.ReplaceCurrentSnapshot(authoritative);
	}

	internal static void Record(Player player, string npcKey, ResidentQuestProgress progress)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		NpcQuestPlayer questPlayer = player.GetModPlayer<NpcQuestPlayer>();
		if (questPlayer.IdentityConfirmed && questPlayer.PersistentId.Length > 0)
		{
			Records[Compose(questPlayer.PersistentId, npcKey)] = progress.Clone();
			questPlayer.Touch();
		}
	}

	private static string Compose(string ownerId, string npcKey) => ownerId + Separator + npcKey;
	private static void Split(string key, out string ownerId, out string npcKey)
	{
		int separator = key.IndexOf(Separator);
		ownerId = separator >= 0 ? key[..separator] : string.Empty;
		npcKey = separator >= 0 ? key[(separator + 1)..] : string.Empty;
	}
}
