using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativePoiDensityReceipt
{
	private static readonly string[] RequiredTargets = [
		"jungle-huts", "sky-lakes", "marble", "granite",
		"cave-houses", "underworld-chests", "cave-chests", "desert-houses",
		"trapped-chests", "sword-shrines", "thin-ice", "campsites",
		"explosive-traps", "living-trees", "long-tracks", "standard-tracks"
	];
	private sealed record Target(double NativeTarget, double TargetCount,
		double Requested, string Scaling, bool Valid);
	private static readonly Dictionary<string, Target> targets = new();
	internal static void Reset() => targets.Clear();
	internal static void Record(string kind, double native, double target,
		string scaling)
	{
		double expected = scaling switch {
			"WorldWidth" or "WorldWidth+Depth" =>
				native * Math.Max(1d, Main.maxTilesY / 2400d),
			"WorldArea" or "None" or "WorldWidth-horizontal" => native,
			"WorldWidthSurface" => Math.Max(native,
				Math.Ceiling(3d * Main.maxTilesX / 8400d)),
			_ => double.NaN
		};
		bool valid = !double.IsNaN(expected) &&
			Math.Abs(Math.Ceiling(target) - Math.Ceiling(expected)) < 0.000001;
		targets[kind] = new Target(native, target, Math.Ceiling(target), scaling, valid);
	}
	internal static bool CoverageComplete
	{
		get
		{
			foreach (string kind in RequiredTargets)
				if (!targets.TryGetValue(kind, out Target target) || !target.Valid)
					return false;
			return true;
		}
	}
	internal static string[] MissingTargets()
	{
		List<string> missing = [];
		foreach (string kind in RequiredTargets)
			if (!targets.TryGetValue(kind, out Target target) || !target.Valid)
				missing.Add(kind);
		return missing.ToArray();
	}

	internal static object Capture()
	{
		Dictionary<string, int> chests = new();
		int[] sectors = new int[32]; // Eight horizontal sectors, four depth bands.
		int physical = 0, stale = 0;
		foreach (Chest chest in Main.chest)
		{
			if (chest == null) continue;
			if (chest.x < 1 || chest.y < 1 || chest.x >= Main.maxTilesX - 1 ||
				chest.y >= Main.maxTilesY - 1) { stale++; continue; }
			Tile tile = Main.tile[chest.x, chest.y];
			if (!tile.HasTile || !TileID.Sets.BasicChest[tile.TileType]) { stale++; continue; }
			physical++;
			string kind = tile.TileType == TileID.Containers
				? "vanilla-style-" + tile.TileFrameX / 36 : "other-container";
			chests[kind] = chests.GetValueOrDefault(kind) + 1;
			int column = Math.Clamp(chest.x * 8 / Main.maxTilesX, 0, 7);
			int band = Math.Clamp((int)((chest.y - Main.worldSurface) * 4 /
				Math.Max(1, Main.maxTilesY - Main.worldSurface)), 0, 3);
			if (chest.y >= Main.worldSurface) sectors[band * 8 + column]++;
		}
		int lakes = 0;
		for (int i = 0; i < Math.Min(GenVars.numIslandHouses, GenVars.skyLake.Length); i++)
			if (GenVars.skyLake[i]) lakes++;
		return new {
			hooks = NativePoiDensitySystem.HookStatus,
			purpose = "all repeatable vanilla configured targets plus surviving chest distribution; not visual acceptance",
			coverageComplete = CoverageComplete,
			requiredTargets = RequiredTargets,
			missingTargets = MissingTargets(),
			targets = new Dictionary<string, Target>(targets),
			jungleHuts = GenVars.numJChests, skyLakes = lakes,
			physicalChestRecords = physical, staleChestRecords = stale, chests,
			undergroundChestSectors = sectors, sectorOrder = "depth-row-major; 4 rows x 8 columns"
		};
	}
}
