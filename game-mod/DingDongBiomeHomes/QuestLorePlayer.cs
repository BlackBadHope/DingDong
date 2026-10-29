using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

/// <summary>
/// Personal, per-character quest presentation state saved in the player file: the last
/// quest stage each resident was talked to about (first-talk lore lines). Tracked
/// residents and codex "read" marks live in BookTrackerPlayer (one store for the book,
/// the HUD tracker and the engine's auto-tracking). Nothing here changes shared world progress.
/// </summary>
public sealed class QuestLorePlayer : ModPlayer
{
    private const int SaveVersion = 1;
    private readonly Dictionary<string, int> dialogueSeen = new(StringComparer.Ordinal);

    internal static string WorldKey()
    {
        Guid id = Main.ActiveWorldFileData?.UniqueId ?? Guid.Empty;
        return id != Guid.Empty ? id.ToString("N") : Main.worldID.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // Marker of the quest state last seen in conversation: Completed * 2 + (active ? 1 : 0).
    internal bool TryConsumeDialogueChange(string npcKey, int marker)
    {
        string key = WorldKey() + "\u001f" + npcKey;
        if (dialogueSeen.TryGetValue(key, out int seen) && seen == marker) return false;
        if (dialogueSeen.Count < 4096 || dialogueSeen.ContainsKey(key)) dialogueSeen[key] = marker;
        return true;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["QuestLoreVersion"] = SaveVersion;
        tag["DialogueSeen"] = dialogueSeen.Select(p => new TagCompound { ["Key"] = p.Key, ["Marker"] = p.Value }).ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        dialogueSeen.Clear();
        if (tag.GetInt("QuestLoreVersion") is < 1 or > SaveVersion) return;
        foreach (var t in tag.GetList<TagCompound>("DialogueSeen").Take(4096)) dialogueSeen[t.GetString("Key")] = t.GetInt("Marker");
    }
}
