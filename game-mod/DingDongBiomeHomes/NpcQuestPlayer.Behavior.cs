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
public sealed partial class NpcQuestPlayer
{
	internal ResidentQuestProgress Get(string npcKey)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return NpcQuestWorldLedger.GetShared(npcKey);
		string key = ComposeKey(CurrentWorldKey(), npcKey);
		if (!progressByWorldAndNpc.TryGetValue(key, out ResidentQuestProgress progress))
		{
			progress = new ResidentQuestProgress();
			progressByWorldAndNpc.Add(key, progress);
		}

		return progress;
	}

	internal bool TryGetActive(out string npcKey, out ResidentQuestProgress progress)
	{
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            var active = NpcQuestWorldLedger.SharedSnapshot(false).FirstOrDefault(p => p.Progress.Active);
            npcKey = active.NpcKey ?? string.Empty; progress = active.Progress; return progress != null;
        }
		string prefix = CurrentWorldKey() + KeySeparator;
		foreach ((string key, ResidentQuestProgress value)in progressByWorldAndNpc)
		{
			if (value.Active && key.StartsWith(prefix, StringComparison.Ordinal))
			{
				npcKey = key[prefix.Length..];
				progress = value;
				return true;
			}
		}

		npcKey = string.Empty;
		progress = null;
		return false;
	}

	internal IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> CurrentActiveRecords()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return NpcQuestWorldLedger.SharedSnapshot(false).Where(p => p.Progress.Active).ToArray();
		string prefix = CurrentWorldKey() + KeySeparator;
		return progressByWorldAndNpc.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal) && pair.Value.Active).Select(pair => (pair.Key[prefix.Length..], pair.Value)).OrderBy(pair => pair.Item1, StringComparer.Ordinal).ToArray();
	}

	internal IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> CurrentSnapshot()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return NpcQuestWorldLedger.SharedSnapshot();
		string prefix = CurrentWorldKey() + KeySeparator;
		return progressByWorldAndNpc.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(pair => (pair.Key[prefix.Length..], pair.Value.Clone())).OrderBy(pair => pair.Item1, StringComparer.Ordinal).ToArray();
	}

	internal bool TryGetCurrent(string npcKey, out ResidentQuestProgress progress)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return NpcQuestWorldLedger.TryGetShared(npcKey, out progress);
		string key = ComposeKey(CurrentWorldKey(), npcKey);
		return progressByWorldAndNpc.TryGetValue(key, out progress);
	}

	internal int CurrentCompletedTotal()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient) return (int)Math.Min(int.MaxValue,NpcQuestWorldLedger.SharedSnapshot(false).Sum(p => (long)p.Progress.Completed));
		string prefix = CurrentWorldKey() + KeySeparator;
		long total = progressByWorldAndNpc.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).Sum(pair => (long)Math.Max(0, pair.Value.Completed));
		return (int)Math.Min(int.MaxValue, total);
	}

	internal void Touch()
	{
		unchecked
		{
			Revision++;
			if (Revision == 0)
				Revision = 1;
		}
	}

	private static string CurrentWorldKey()
	{
		Guid uniqueId = Main.ActiveWorldFileData?.UniqueId ?? Guid.Empty;
		return uniqueId != Guid.Empty ? uniqueId.ToString("N") : Main.worldID.ToString(CultureInfo.InvariantCulture);
	}

	private static string ComposeKey(string worldKey, string npcKey) => worldKey + KeySeparator + npcKey;
	private static void SplitKey(string key, out string worldKey, out string npcKey)
	{
		int separator = key.IndexOf(KeySeparator);
		worldKey = separator >= 0 ? key[..separator] : string.Empty;
		npcKey = separator >= 0 ? key[(separator + 1)..] : string.Empty;
	}
}
