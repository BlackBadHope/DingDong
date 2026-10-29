using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes;

/// <summary>
/// Owner acceptance item 11 (28.10, "use a randomizer"): every world draws a
/// fixed number of resident stories as the main storyline, four fifths of them
/// vanilla residents, the rest from the pack's mods. The draw is seeded by the
/// world id, which both the server and every joining client know, so the book
/// marks the same residents everywhere without extra sync or save data. The
/// other residents keep their stories as side lines.
/// </summary>
internal static class ResidentStoryRoles
{
	internal const int MainCount = 20;
	internal const int MainVanillaCount = 16; // 80 % of MainCount
	private const int SeedSalt = 0x5D1D0A6;
	/// <summary>Nominal size of the main line in stages (MainCount stories of five stages).</summary>
	internal const int NominalMainStages = MainCount * 5;
	// Residents that may never move in even with their mod loaded (MagicStorage's Automaton
	// arrives only when that mod's server setting allows it). A main line must stay finishable,
	// so they are never drawn as story residents and keep their stories as side lines.
	private static readonly HashSet<string> ConditionalResidents = new(StringComparer.Ordinal) { "MagicStorage/Golem" };
	private static int cachedWorld = int.MinValue;
	private static HashSet<string> cached;

	internal static bool IsVanilla(string npcKey) => npcKey != null && npcKey.StartsWith("Terraria/", StringComparison.Ordinal);

	internal static bool IsMain(string npcKey) => !Main.gameMenu && MainSet(Main.worldID).Contains(npcKey);

	internal static HashSet<string> MainSet(int worldId)
	{
		if (cached != null && cachedWorld == worldId)
			return cached;
		cached = Draw(worldId, NpcTownManifest.All.Select(n => n.NpcKey).Where(EligibleForMain));
		cachedWorld = worldId;
		return cached;
	}

	internal static HashSet<string> Draw(int worldId, IEnumerable<string> keys)
	{
		List<string> ordered = keys.Where(k => !string.IsNullOrEmpty(k)).Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal).ToList();
		List<string> vanilla = ordered.Where(IsVanilla).ToList();
		List<string> modded = ordered.Where(k => !IsVanilla(k)).ToList();
		Random rng = new(unchecked(worldId * 73856093) ^ SeedSalt);
		HashSet<string> set = new(StringComparer.Ordinal);
		Take(rng, vanilla, Math.Min(MainVanillaCount, vanilla.Count), set);
		Take(rng, modded, Math.Min(MainCount - set.Count, modded.Count), set);
		Take(rng, vanilla, Math.Min(MainCount - set.Count, vanilla.Count), set); // too few modded residents: fill from vanilla
		return set;
	}

	/// <summary>Only residents that can actually live in this world: their NPC is loaded and no external setting gates them.</summary>
	internal static bool EligibleForMain(string npcKey) =>
		!ConditionalResidents.Contains(npcKey) && NpcResidentRegistry.TryGetType(npcKey, out _);

	private static void Take(Random rng, List<string> pool, int count, HashSet<string> into)
	{
		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			int index = rng.Next(pool.Count);
			into.Add(pool[index]);
			pool.RemoveAt(index);
		}
	}

	internal static void Reset()
	{
		cached = null;
		cachedWorld = int.MinValue;
	}

	internal static int RunChecks()
	{
		string[] keys = NpcTownManifest.All.Select(n => n.NpcKey).ToArray();
		if (keys.Length != 61) throw new InvalidOperationException("Story roles expect the 61 authored residents, found " + keys.Length);
		HashSet<string> a = Draw(123456789, keys), again = Draw(123456789, keys), b = Draw(987654321, keys);
		if (a.Count != MainCount || a.Count(IsVanilla) != MainVanillaCount) throw new InvalidOperationException($"Main draw must be {MainCount} with {MainVanillaCount} vanilla, got {a.Count}/{a.Count(IsVanilla)}");
		if (!a.SetEquals(again)) throw new InvalidOperationException("Main draw must be deterministic per world id");
		if (a.SetEquals(b)) throw new InvalidOperationException("Different worlds should draw different main lines");
		if (a.Any(k => !keys.Contains(k))) throw new InvalidOperationException("Main draw contains an unknown resident");
		return 4;
	}
}
