using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Immutable inventory of third-party authored geometry embedded in the mod.
/// This is deliberately separate from the NPC manifest: a theme name or a set
/// of procedural parameters is not a building preset.
/// </summary>
public static partial class AuthoredPresetCatalog
{
	public static bool TryGet(string id, out AuthoredPreset preset) => Entries.TryGetValue(id, out preset);
	private static void AddImported(Dictionary<string, AuthoredPreset> entries, IReadOnlyList<AuthoredPreset> source, string id)
	{
		foreach (AuthoredPreset preset in source)
		{
			if (!string.Equals(preset.Id, id, StringComparison.Ordinal))
				continue;
			if (!entries.TryAdd(id, preset))
				throw new InvalidOperationException($"Duplicate authored preset id: {id}");
			return;
		}

		throw new InvalidOperationException($"Imported authored preset does not exist: {id}");
	}

	private static void Add(Dictionary<string, AuthoredPreset> entries, string id, int width, int height, int groundOffset, int housingCapacity, string author, PresetRole roles, bool containsNbt = false)
	{
		AuthoredPreset preset = new(id, $"Structures/Structify/{id}.shstruct", width, height, groundOffset, housingCapacity, author, StructifySource, StructifyCommit, StructifyLicense, roles, AuthoredPlacementMode.SurfaceStructure, true, containsNbt);
		if (!entries.TryAdd(id, preset))
			throw new InvalidOperationException($"Duplicate authored preset id: {id}");
	}

	private static void AddPrivateHome(Dictionary<string, AuthoredPreset> entries, string id, string fileName, int width, int height, AuthoredPlacementMode placementMode, PresetRole roles)
	{
		AuthoredPreset preset = new(id, $"Structures/PrivateHome/ZiiioNpcThemed/{fileName}.shstruct", width, height, null, 0, "Ziiio", ZiiioSource, $"wld-sha256:{ZiiioWorldSha256}", PrivateHomeLicense, roles | PresetRole.PrivateHome, placementMode, false, false);
		if (!entries.TryAdd(id, preset))
			throw new InvalidOperationException($"Duplicate authored preset id: {id}");
	}

	private static void AddGreaveil(Dictionary<string, AuthoredPreset> entries, string id, string fileName, int width, int height, AuthoredPlacementMode placementMode, PresetRole roles)
	{
		AuthoredPreset preset = new(id, $"Structures/PrivateHome/Greaveil/{fileName}.shstruct", width, height, null, 0, "TUNA DASH", GreaveilSource, $"wld-sha256:{GreaveilWorldSha256}", PrivateHomeLicense, roles | PresetRole.PrivateHome, placementMode, false, false);
		if (!entries.TryAdd(id, preset))
			throw new InvalidOperationException($"Duplicate authored preset id: {id}");
	}

	private static void AddAegis8(Dictionary<string, AuthoredPreset> entries, string id, string fileName, int width, int height, AuthoredPlacementMode placementMode, PresetRole roles)
	{
		AuthoredPreset preset = new(id, $"Structures/PrivateHome/Aegis8/{fileName}.shstruct", width, height, null, 0, "TerraMonolith", Aegis8Source, $"wld-sha256:{Aegis8WorldSha256}", PrivateHomeLicense, roles | PresetRole.PrivateHome, placementMode, false, false);
		if (!entries.TryAdd(id, preset))
			throw new InvalidOperationException($"Duplicate authored preset id: {id}");
	}
}
