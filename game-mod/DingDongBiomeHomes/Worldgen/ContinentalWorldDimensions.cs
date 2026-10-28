using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

// Presets 28.10: one continental world preset per RAM tier. ram32 is the proven x10
// continent (28000x7200); smaller presets are registered but cannot be CREATED (menu, server
// variable) until their server run passes 61/61 (ResidencesProven) or DINGDONG_PRESET_PREVIEW=1
// is set for that run. Loading/joining recognises them by size (review fix, see IsRecognized).
// Offered=false keeps a preset hidden even in preview (ram8, owner decision B1).
internal sealed record ContinentalWorldPreset(
	string Id,
	int Width,
	int Height,
	int RamTierGb,
	int Order,
	bool ResidencesProven,
	bool Offered,
	string ReceiptId)
{
	internal int StorageWidth => Width + 1;
	internal int StorageHeight => Height + 1;
	internal long TileCount => (long)Width * Height;
	internal long StorageCellCount => (long)StorageWidth * StorageHeight;
	internal string LocKey => "Mods.DingDongBiomeHomes.WorldCreation.Presets." + Id;
	// Presets 28.10: adapter receipt tag "<id>-<W>x<H>"; ram32 keeps exactly "x10-28000x7200".
	internal string ReceiptTag => $"{ReceiptId}-{Width}x{Height}";
	internal bool IsX10 => Width == ContinentalWorldDimensions.Width && Height == ContinentalWorldDimensions.Height;
}

internal static class ContinentalWorldDimensions
{
	internal const int VanillaLargeWidth = 8400;
	internal const int VanillaLargeHeight = 2400;
	// x10 constants stay as the ram32 preset fields (fixtures, probes, x10-only compiler).
	internal const int Width = 28000;
	internal const int Height = 7200;
	internal const int StorageWidth = Width + 1;
	internal const int StorageHeight = Height + 1;
	internal const long TileCount = (long)Width * Height;
	internal const long StorageCellCount = (long)StorageWidth * StorageHeight;

	// Presets 28.10: preview switch for presets that have not passed their server run yet.
	internal const string PreviewVariable = "DINGDONG_PRESET_PREVIEW";
	internal static readonly bool PreviewEnabled = string.Equals(
		Environment.GetEnvironmentVariable(PreviewVariable)?.Trim(), "1", StringComparison.Ordinal);

	// Presets 28.10: the preset table (design WORLD-PRESETS-DESIGN-20261028, section 2; B1/B6).
	internal static readonly ContinentalWorldPreset X10 =
		new("ram32", Width, Height, 32, 4, ResidencesProven: true, Offered: true, ReceiptId: "x10");
	internal static readonly IReadOnlyList<ContinentalWorldPreset> Presets = new[]
	{
		new ContinentalWorldPreset("ram8", 14000, 4200, 8, 0, ResidencesProven: false, Offered: false, ReceiptId: "ram8"),
		new ContinentalWorldPreset("ram12", 16800, 4800, 12, 1, ResidencesProven: false, Offered: true, ReceiptId: "ram12"),
		new ContinentalWorldPreset("ram16", 20400, 6000, 16, 2, ResidencesProven: false, Offered: true, ReceiptId: "ram16"),
		new ContinentalWorldPreset("ram24", 23800, 6600, 24, 3, ResidencesProven: false, Offered: true, ReceiptId: "ram24"),
		X10,
	};

	// Presets 28.10: a preset is usable when it is proven, or offered and previewed.
	internal static bool IsAvailable(ContinentalWorldPreset preset) =>
		preset != null && (preset.ResidencesProven || PreviewEnabled && preset.Offered);

	internal static IReadOnlyList<ContinentalWorldPreset> Available =>
		Presets.Where(IsAvailable).OrderBy(p => p.Order).ToArray();

	// Presets 28.10 review fix: an existing world is recognised by its size alone (proven or
	// offered preset), independent of DINGDONG_PRESET_PREVIEW. The preview gate applies only to
	// creation (Available, ResolveRequestedPreset). Otherwise a client without preview joining a
	// previewed ram16 server, or a previewed world reopened without the variable, would run in
	// vanilla mode (no admission, no section streaming, no Hell v2) while the size is continental.
	// ram8 (Offered=false) can never be created, so a 14000x4200 world stays foreign/vanilla.
	internal static bool IsRecognized(ContinentalWorldPreset preset) =>
		preset != null && (preset.ResidencesProven || preset.Offered);

	// Presets 28.10: vanilla 4200x1200, 6400x1800 and 8400x2400 are never in the table.
	internal static ContinentalWorldPreset TryResolve(int width, int height)
	{
		foreach (ContinentalWorldPreset preset in Presets)
			if (preset.Width == width && preset.Height == height)
				return IsRecognized(preset) ? preset : null;
		return null;
	}

	internal static ContinentalWorldPreset FindById(string id) =>
		string.IsNullOrWhiteSpace(id) ? null :
		Presets.FirstOrDefault(p => p.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));

	private sealed record ResolveCache(int Width, int Height, ContinentalWorldPreset Preset);
	private static volatile ResolveCache resolveCache;

	// Presets 28.10: preset of the active world (null on vanilla sizes). IsContinental is read
	// by ~45 gates, so the last answer is cached per (maxTilesX, maxTilesY).
	internal static ContinentalWorldPreset Current
	{
		get
		{
			int width = Main.maxTilesX, height = Main.maxTilesY;
			ResolveCache cache = resolveCache;
			if (cache != null && cache.Width == width && cache.Height == height)
				return cache.Preset;
			ContinentalWorldPreset preset = TryResolve(width, height);
			resolveCache = new ResolveCache(width, height, preset);
			return preset;
		}
	}

	// Presets 28.10: largest available preset whose tier fits total physical RAM (Windows reports
	// a little less than the installed size, hence 90 %). Nothing fits: the smallest available
	// one (still clickable, with a warning). RAM unknown: the largest (previous x10 behaviour).
	// Client menu only: the dedicated server uses RecommendFitting (review fix).
	internal static ContinentalWorldPreset Recommend(long? totalPhysicalBytes)
	{
		IReadOnlyList<ContinentalWorldPreset> available = Available;
		if (available.Count == 0)
			return null;
		if (totalPhysicalBytes is not long physical || physical <= 0)
			return available[^1];
		ContinentalWorldPreset best = null;
		foreach (ContinentalWorldPreset preset in available)
			if (FitsPhysical(preset, physical))
				best = preset;
		return best ?? available[0];
	}

	// Presets 28.10 review fix: dedicated server default. Only a preset that fits physical RAM;
	// RAM unknown or nothing fits -> null (no promotion, the pre-preset vanilla behaviour), so a
	// host below the x10 tier is never silently pushed into an x10 memory denial.
	internal static ContinentalWorldPreset RecommendFitting(long? totalPhysicalBytes)
	{
		if (totalPhysicalBytes is not long physical || physical <= 0)
			return null;
		ContinentalWorldPreset best = Recommend(physical);
		return best != null && FitsPhysical(best, physical) ? best : null;
	}

	internal static bool FitsPhysical(ContinentalWorldPreset preset, long totalPhysicalBytes) =>
		totalPhysicalBytes >= (long)(preset.RamTierGb * 0.9 * 1024d * 1024d * 1024d);

	// Presets 28.10: next smaller available preset (memory-denial hints), or null.
	internal static ContinentalWorldPreset NextSmaller(ContinentalWorldPreset preset) =>
		preset == null ? null :
		Available.Where(p => p.Order < preset.Order).OrderBy(p => p.Order).LastOrDefault();

	internal static bool IsVanillaLarge =>
		Main.maxTilesX == VanillaLargeWidth && Main.maxTilesY == VanillaLargeHeight;

	// Presets 28.10: vanilla Small/Medium/Large (carrier sizes for promotion).
	internal static bool IsVanillaSize(int width, int height) =>
		width == 4200 && height == 1200 || width == 6400 && height == 1800 ||
		width == VanillaLargeWidth && height == VanillaLargeHeight;

	internal static bool IsContinental => Current != null;
	// Presets 28.10: exactly the x10 continent (experimental compiler and x10-only budgets).
	internal static bool IsX10 => Main.maxTilesX == Width && Main.maxTilesY == Height;
	internal static bool IsDeploymentWorld => IsVanillaLarge || IsContinental;

	// Presets 28.10 (design step 10): authored settlements only on a continental preset.
	internal static bool SupportsAuthoredSettlements => IsContinental;

	internal static void Validate()
	{
		long vanillaTiles = (long)VanillaLargeWidth * VanillaLargeHeight;
		if (TileCount != vanillaTiles * 10L || X10.TileCount != TileCount)
			throw new InvalidOperationException("Continental dimensions must equal ten Large-world areas.");
		// Presets 28.10: every preset gets the same storage/wire/section checks as x10.
		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (ContinentalWorldPreset preset in Presets)
		{
			if (!ids.Add(preset.Id))
				throw new InvalidOperationException($"Duplicate continental preset id {preset.Id}.");
			if (IsVanillaSize(preset.Width, preset.Height))
				throw new InvalidOperationException($"Continental preset {preset.Id} must not use a vanilla size.");
			if (preset.Width >= short.MaxValue || preset.Height >= short.MaxValue ||
				preset.StorageWidth > ushort.MaxValue || preset.StorageHeight > ushort.MaxValue)
				throw new InvalidOperationException($"Continental storage of {preset.Id} exceeds signed multiplayer coordinates.");
			if (preset.StorageCellCount > int.MaxValue || preset.StorageCellCount > Array.MaxLength)
				throw new InvalidOperationException($"Continental Tilemap length of {preset.Id} exceeds CLR array indexing capacity.");
			if (preset.Width % Main.sectionWidth != 0 || preset.Height % Main.sectionHeight != 0)
				throw new InvalidOperationException($"Continental dimensions of {preset.Id} must align to network sections.");
			if (preset.TileCount > TileCount || preset.Width <= VanillaLargeWidth || preset.Height <= VanillaLargeHeight)
				throw new InvalidOperationException($"Continental preset {preset.Id} must lie between Large and x10.");
		}
	}
}
