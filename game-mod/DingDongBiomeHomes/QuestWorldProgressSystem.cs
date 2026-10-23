using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed class QuestWorldProgressSystem : ModSystem
{
	private static readonly HashSet<string> DefeatedBosses = new(StringComparer.Ordinal);
	internal static int DefeatedBossCount => DefeatedBosses.Count;

	internal static bool WasBossDefeated(string contentKey) => DefeatedBosses.Contains(contentKey);
	internal static void RecordBoss(NPC npc)
	{
		if (npc.boss)
			DefeatedBosses.Add(QuestContentRegistry.ContentKey(npc));
	}

	public override void ClearWorld() => DefeatedBosses.Clear();
	public override void OnWorldUnload() => DefeatedBosses.Clear();
	public override void SaveWorldData(TagCompound tag)
	{
		if (DefeatedBosses.Count > 0)
			tag["QuestDefeatedBosses"] = DefeatedBosses.OrderBy(value => value, StringComparer.Ordinal).ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		DefeatedBosses.Clear();
		if (!tag.ContainsKey("QuestDefeatedBosses"))
			return;
		foreach (string key in tag.GetList<string>("QuestDefeatedBosses"))
		{
			if (!string.IsNullOrWhiteSpace(key))
				DefeatedBosses.Add(key);
		}
	}
}
