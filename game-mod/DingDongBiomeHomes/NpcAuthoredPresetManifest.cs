using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Explicit NPC-to-geometry assignments. This manifest is intentionally
/// fail-closed: world generation stays disabled until all 61 supported NPCs
/// have distinct reviewed authored presets.
/// </summary>
public static partial class NpcAuthoredPresetManifest
{
	private static readonly Dictionary<string, string> CandidateEntries = BuildCandidates();
	private static readonly Dictionary<string, string> Entries = new(StringComparer.Ordinal);
	public static int Count => Entries.Count;
	public static int CandidateCount => CandidateEntries.Count;
	public static bool IsComplete => Entries.Count == NpcTownManifest.AuthoredCount;
	public static bool IsCandidateComplete => CandidateEntries.Count == NpcTownManifest.AuthoredCount;
}
