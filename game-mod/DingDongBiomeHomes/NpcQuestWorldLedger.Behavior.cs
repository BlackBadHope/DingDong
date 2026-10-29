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
        var q = player.GetModPlayer<NpcQuestPlayer>();
        if (!q.IdentityConfirmed || q.PersistentId.Length == 0) return;
        MigrateLegacyRecords();
        if (Main.netMode != NetmodeID.MultiplayerClient && ResumeClaims(q.PersistentId)) { PublishShared(); NpcQuestNetwork.SendQuestSnapshot(null, -1); }
        q.ReplaceCurrentSnapshot(SharedSnapshot());
    }
    internal static void Record(Player player, string npcKey, ResidentQuestProgress progress)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !player.GetModPlayer<NpcQuestPlayer>().IdentityConfirmed) return;
        Shared[npcKey] = progress;
        PublishShared();
    }
	private static string Compose(string ownerId, string npcKey) => ownerId + Separator + npcKey;
	private static void Split(string key, out string ownerId, out string npcKey)
	{
		int separator = key.IndexOf(Separator);
		ownerId = separator >= 0 ? key[..separator] : string.Empty;
		npcKey = separator >= 0 ? key[(separator + 1)..] : string.Empty;
	}
}
