using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Persistent per-item origin and element metadata. These values travel with
/// the concrete item stack instead of being recomputed from the current owner.
/// </summary>
public sealed partial class DamageAffinityGlobalItem
{
	internal static string ResolveSpawnAcquisition(IEntitySource source, string fallback) => source switch
	{
		EntitySource_Loot loot => ResolveLootAcquisition(loot.Entity, fallback),
		EntitySource_FishedOut => DamageTags.Fishing,
		EntitySource_ShakeTree or EntitySource_WorldGen => DamageTags.Worldgen,
		EntitySource_Gift => DamageTags.Quest,
		EntitySource_ItemOpen opened => ResolveOpenedItemAcquisition(opened.ItemType, fallback),
		EntitySource_WorldEvent => DamageTags.Event,
		_ => fallback
	};
	internal static string ResolveLootAcquisition(Entity source, string fallback) => source switch
	{
		NPC npc when npc.boss => DamageTags.BossDrop,
		NPC => DamageTags.CreatureDrop,
		_ => fallback
	};
	internal static string ResolveOpenedItemAcquisition(int itemType, string fallback)
	{
		if (itemType >= 0 && itemType < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[itemType])
			return DamageTags.BossDrop;
		if (itemType >= 0 && itemType < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[itemType])
			return DamageTags.Fishing;
		return fallback;
	}
}
