using System;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastPlacement
{
	internal static string CurrentPass { get; set; }
	private static bool coastsComplete;
	private static bool Active => NativeCoastalSystem.Active && FishingWaterWorldSystem.Generating && !coastsComplete;
	internal static void Reset() { CurrentPass = null; coastsComplete = false; }
	internal static void AfterPass(string name) { if (name == "Sulphur Sea 2") coastsComplete = true; }
	private static int Width => FishingWaterGeometry.OceanWidth(Main.maxTilesX, (int)(Main.maxTilesX / 16.8f));
	internal static void BeforePass(string name)
	{
		if (!Active || name != "Dungeon") return;
		int previous = GenVars.dungeonLocation;
		GenVars.dungeonLocation = NativeCoastPlacementPolicy.DungeonAnchor(previous, Main.maxTilesX, Width);
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_COAST_DUNGEON|original={previous}|anchor={GenVars.dungeonLocation}|futureOceanWidth={Width}|nativeDungeonGenerator=True");
	}
	internal static bool PermitChest(int x, int y) => NativeSanctuaryNativeAdmission.PermitChest(x, y) &&
		(!Active || CurrentPass != "Buried Chests" ||
		y >= NativeUnderworldSystem.Layer || !NativeCoastPlacementPolicy.InCoast(x - 2, x + 2, Main.maxTilesX, Width + 64, 0));
	internal static bool PermitStructure(Rectangle area, int padding)
	{
		if (!Active || CurrentPass == null || CurrentPass is
			"DingDong ocean expeditions" or "Sulphur Sea" or "Sulphur Sea 2" or "Abyss" or
			"SOTS: Ocean Caves" or "Thorium Mod: Aquatic Depths" or
			// V03 29.10: the shore seal writes plugs on the coast; without this every
			// coastal 1x1 CanPlace is false before "Sulphur Sea 2" and only self-seals ran.
			"DingDong shore seal" or "DingDong shore seal again" or "DingDong shore seal coast") return true;
		// Allow sky structures above the sea's air-clearance envelope, and Hell
		// structures below the future Abyss. All existing provider reservations
		// remain intact; this only rejects a candidate before it writes tiles.
		int top = Math.Max(40, (int)GenVars.worldSurfaceLow - NativeCoastalPolicy.Clearance(150, Main.maxTilesY, true));
		if (area.Bottom + padding < top || area.Top - padding >= NativeUnderworldSystem.Layer) return true;
		return !NativeCoastPlacementPolicy.InCoast(area.Left, area.Right, Main.maxTilesX,
			Width + NativeCoastalPolicy.BeachAndGraveyardMargin, padding);
	}
}
