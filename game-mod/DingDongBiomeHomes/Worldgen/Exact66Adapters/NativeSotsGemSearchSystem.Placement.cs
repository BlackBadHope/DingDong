using System;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

public sealed partial class NativeSotsGemSearchSystem
{
	private void PlaceAmber(Original original)
	{
		if (!Active) { original(); return; }
		var clock = Stopwatch.StartNew();
		bool right = Main.dungeonX > Main.maxTilesX / 2;
		if (calamity) right = !right;
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_START|gem=Amber|right={right}|bottom={Main.maxTilesY - 50}");
		if (!SotsAmberAnchorSearch.TryFind(Main.maxTilesX, Main.maxTilesY - 50,
			right, IsSand, IsWater, out int x, out int y) &&
			!SotsAmberAnchorSearch.TryFind(Main.maxTilesX, Main.maxTilesY - 50,
				right, IsSand, IsWater, out x, out y, span: Main.maxTilesX / 6))
		{
			// V03 29.10: a missing anchor must not abort a 30-minute world; the gap is logged loudly.
			Mod.Logger.Error($"DINGDONG_NATIVE_GEM_SKIPPED|gem=Amber|reason=no-water-over-sand-anchor|side={(right ? "right" : "left")}|depth={Main.maxTilesY}|elapsedMs={clock.ElapsedMilliseconds}");
			return;
		}
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_ANCHOR|gem=Amber|x={x}|y={y}|searchMs={clock.ElapsedMilliseconds}");
		OceanExpeditions.OceanForeignFootprint.AmberAnchor = new(x, y); // V03 29.10: intended sea-water vault.
		amber(x, y); // Original authored SOTS structure, not a surrogate.
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_END|gem=Amber|elapsedMs={clock.ElapsedMilliseconds}");
	}

	private bool IsWater(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return !solid(x, y, false) && tile.LiquidType == LiquidID.Water &&
			tile.LiquidAmount > 200;
	}

	private static bool IsSand(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && tile.TileType is
			TileID.Sand or TileID.Ebonsand or TileID.Crimsand or TileID.Pearlsand;
	}

	private void PlaceAmethyst(Original original)
	{
		if (!Active) { original(); return; }
		var clock = Stopwatch.StartNew();
		int left = GenVars.UndergroundDesertLocation.X;
		int right = GenVars.UndergroundDesertLocation.Right;
		if (right - left < 10) (left, right) = (500, Main.maxTilesX - 500);
		left = Math.Max(50, left);
		right = Math.Min(Main.maxTilesX - 50, right);
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_START|gem=Amethyst|left={left}|right={right}");
		if (!SotsAmethystAnchorSearch.TryFind(left, right, FirstDesertEvent,
			out int x, out int y))
			throw new InvalidOperationException(
				$"SOTS Amethyst: no valid sand camp anchor in [{left},{right}); no structure placed at Y=0.");
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_ANCHOR|gem=Amethyst|x={x}|y={y}|searchMs={clock.ElapsedMilliseconds}");
		amethyst(x, y);
		Mod.Logger.Info($"DINGDONG_NATIVE_GEM_END|gem=Amethyst|elapsedMs={clock.ElapsedMilliseconds}");
	}

	private int FirstDesertEvent(int x)
	{
		for (int y = 100; y < Main.maxTilesY / 2; y++)
		{
			Tile tile = Main.tile[x, y];
			Tile below = Main.tile[x, y + 20];
			if ((tile.HasTile && tile.TileType == pyramid) ||
				(below.HasTile && below.TileType == pyramid)) return -1;
			if (tile.HasTile && tile.TileType == TileID.Sand) return y;
		}
		return 0;
	}
}
