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
	public static bool TryGetCandidate(string npcKey, out AuthoredPreset preset)
	{
		preset = null;
		return CandidateEntries.TryGetValue(npcKey, out string presetId) && AuthoredPresetCatalog.TryGet(presetId, out preset);
	}
}
