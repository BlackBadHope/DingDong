using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal sealed class MobRelicAuditContext
{
	internal MobRelicAuditContext(Mod mod)
	{
		Mod = mod;
		EligibleMobs = ContentSamples.NpcsByNetId.Values.Where(MobRelicGlobalNPC.IsEligible).GroupBy(QuestContentRegistry.ContentKey, StringComparer.Ordinal).Select(group => group.First()).OrderBy(QuestContentRegistry.ContentKey, StringComparer.Ordinal).ToList();
		Sets = MobSpecificRelicCatalog.RegisteredSets;
		RegisteredItems = MobSpecificRelicCatalog.RegisteredItems;
		Drops = MobRelicGlobalNPC.RegisteredDrops;
		Generated = MobRelicSourceCatalog.All.ToDictionary(source => source.ContentKey, StringComparer.Ordinal);
		GeneratedKeys = Generated.Keys.ToHashSet(StringComparer.Ordinal);
		LoadedMobs = EligibleMobs.Where(mob => GeneratedKeys.Contains(QuestContentRegistry.ContentKey(mob))).ToList();
		LoadedKeys = LoadedMobs.Select(QuestContentRegistry.ContentKey).ToHashSet(StringComparer.Ordinal);
	}

	internal Mod Mod { get; }
	internal List<NPC> EligibleMobs { get; }
	internal IReadOnlyDictionary<string, MobRelicSet> Sets { get; }
	internal IReadOnlyList<GeneratedMobRelicItem> RegisteredItems { get; }
	internal IReadOnlyDictionary<string, MobRelicDropRegistration> Drops { get; }
	internal Dictionary<string, MobRelicSourceDefinition> Generated { get; }
	internal HashSet<string> GeneratedKeys { get; }
	internal List<NPC> LoadedMobs { get; }
	internal HashSet<string> LoadedKeys { get; }
	internal List<string> Failures { get; } = new();
	internal HashSet<int> UniqueTypes { get; } = new();
	internal HashSet<string> UniqueNames { get; } = new(StringComparer.Ordinal);
	internal HashSet<string> UniqueTexturePaths { get; } = new(StringComparer.Ordinal);
	internal HashSet<string> UniqueVisualSignatures { get; } = new(StringComparer.Ordinal);
	internal HashSet<string> UniqueMechanics { get; } = new(StringComparer.Ordinal);
	internal HashSet<MobRelicTrigger> UniqueTriggers { get; } = new();
	internal HashSet<MobRelicInvocation> UniqueInvocations { get; } = new();
	internal HashSet<MobRelicTwist> UniqueTwists { get; } = new();
	internal int PoolsPassed;
	internal int ItemsPassed;
	internal int SaveLoadPassed;
	internal int NetPassed;
	internal int QuestIdentityPassed;
	internal int FunctionalItemsPassed;
	internal int PlayerVisualsPassed;
	internal int ResonanceProjectilePassed;
	internal int RuntimeGameplayPassed;
	internal int LootDatabasePassed;
	internal int ReportedDropsPassed;
	internal int ClonePassed;
	internal int StackPassed;
	internal int WorldStackPassed;
	internal int ContentSamplesPassed;
	internal int ForeignRulesEnumerated;
	internal int TexturesPassed;
	internal int LocalizationPassed;
}
