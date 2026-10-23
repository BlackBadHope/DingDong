using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;

/// <summary>Own public replacements. Legacy IDs retain assignment compatibility, not source geometry.</summary>
public static class PublicHomeReplacementCatalog
{
	public const string Prefix = "Procedural/PublicHomes/v1/";
	public const string Revision = "dingdong-original-homes-v1";
	private static readonly Dictionary<string, AuthoredPreset> Presets = new(StringComparer.Ordinal);
	private static readonly object Gate = new();

	public static AuthoredPreset Create(string id, int width, int height,
		AuthoredPlacementMode placementMode, PresetRole roles)
	{
		if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
			throw new ArgumentException("A public home needs a simple legacy identifier.", nameof(id));
		// The plan validates dimensions before any tile-data buffers are allocated.
		PublicHomeReplacementPlan.ValidateDimensions(width, height);
		int capacity = PublicHomeReplacementPlan.RoomCount(id, width, height);
		bool compact = placementMode == AuthoredPlacementMode.SurfaceStructure;
		AuthoredPreset preset = new(id, Prefix + id, width, height,
			compact ? 3 : null, capacity, "DingDong contributors", "original-procedural",
			Revision, "PROJECT_OWNED; public distribution authorized; final license pending",
			roles & ~PresetRole.PrivateHome, placementMode, compact, false);
		lock (Gate)
		{
			if (Presets.TryGetValue(preset.ResourcePath, out AuthoredPreset existing) && existing != preset)
				throw new InvalidOperationException($"Conflicting public home metadata: {id}.");
			Presets[preset.ResourcePath] = preset;
		}
		return preset;
	}

	public static bool IsProcedural(string path) => path?.StartsWith(Prefix, StringComparison.Ordinal) == true;

	public static bool IsForbiddenLegacyResource(string path) =>
		path?.Replace('\\', '/').StartsWith("Structures/PrivateHome/", StringComparison.OrdinalIgnoreCase) == true;

	public static bool TryResolve(string path, out AuthoredPreset preset)
	{
		// The runtime catalogue also initializes every intake list used by imports.
		// No source files, image, world or .shstruct payload is accessed here.
		_ = AuthoredPresetCatalog.All.Count;
		lock (Gate) return Presets.TryGetValue(path, out preset);
	}

	public static IReadOnlyDictionary<string, AuthoredPreset> All
	{
		get
		{
			_ = AuthoredPresetCatalog.All.Count;
			lock (Gate) return new ReadOnlyDictionary<string, AuthoredPreset>(
				new Dictionary<string, AuthoredPreset>(Presets, StringComparer.Ordinal));
		}
	}
}
