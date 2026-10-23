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
	private const string ModBiomePrefix = "ModBiome:";
	private static readonly List<UniversalQuestTemplate> Templates = new();
	private static readonly Dictionary<string, ModBiome> ModBiomes = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, NPC> NpcTargets = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, HashSet<string>> BossTargetsByMod = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, Func<bool>> ExternalMilestones = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, string> ExternalMilestoneNames = new(StringComparer.Ordinal);
	private static bool initialized;
	private static ulong catalogFingerprint;
	internal static int TemplateCount
	{
		get
		{
			EnsureInitialized();
			return Templates.Count;
		}
	}

	internal static ulong CatalogFingerprint
	{
		get
		{
			EnsureInitialized();
			return catalogFingerprint;
		}
	}
}
