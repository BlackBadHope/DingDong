using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
/// <summary>
/// Builds quest targets from the content that is actually loaded. This keeps
/// boss, banner and biome quests compatible with content mods without taking a
/// hard assembly dependency on each of them.
/// </summary>
internal static partial class QuestContentRegistry
{
	internal static IReadOnlyList<UniversalQuestTemplate> SnapshotTemplates()
	{
		EnsureInitialized();
		return Templates.ToArray();
	}

	internal static string ContentKey(NPC npc)
	{
		if (npc.ModNPC != null)
			return npc.ModNPC.FullName;
		string vanillaName = NPCID.Search.GetName(npc.type);
		return string.IsNullOrWhiteSpace(vanillaName) ? $"Terraria/NPC_{npc.type}" : $"Terraria/{vanillaName}";
	}

	internal static string ContentMod(NPC npc) => npc.ModNPC?.Mod?.Name ?? "Terraria";
	internal static bool PlayerInBiome(Player player, string biomeKey)
	{
		if (string.IsNullOrWhiteSpace(biomeKey))
			return false;
		if (biomeKey.StartsWith(ModBiomePrefix, StringComparison.Ordinal))
		{
			EnsureInitialized();
			return ModBiomes.TryGetValue(biomeKey, out ModBiome biome) && player.InModBiome(biome);
		}

		return biomeKey switch
		{
			"Forest" => player.ZoneOverworldHeight && !player.ZoneSnow && !player.ZoneDesert && !player.ZoneJungle && !player.ZoneBeach && !player.ZoneHallow && !player.ZoneGlowshroom,
			"Snow" => player.ZoneSnow,
			"Desert" => player.ZoneDesert,
			"Jungle" => player.ZoneJungle,
			"Ocean" => player.ZoneBeach,
			"Underground" => player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight,
			"Hallow" => player.ZoneHallow,
			"Mushroom" => player.ZoneGlowshroom,
			"Underworld" => player.ZoneUnderworldHeight,
			_ => false
		};
	}

	internal static bool TargetExists(ResidentQuestDefinition definition)
	{
		EnsureInitialized();
		return definition.Kind switch
		{
			ResidentQuestKind.BossHunt or ResidentQuestKind.BannerCollection => NpcTargets.ContainsKey(definition.TargetKey),
			ResidentQuestKind.BiomePurge => !definition.TargetKey.StartsWith(ModBiomePrefix, StringComparison.Ordinal) || ModBiomes.ContainsKey(definition.TargetKey),
			ResidentQuestKind.ModProgression => definition.TargetKey.StartsWith("milestone:", StringComparison.Ordinal) ? ExternalMilestones.ContainsKey(definition.TargetKey) : ModLoader.TryGetMod(definition.TargetKey, out _) && HasUndefeatedBoss(definition.TargetKey),
			_ => true
		};
	}
}
