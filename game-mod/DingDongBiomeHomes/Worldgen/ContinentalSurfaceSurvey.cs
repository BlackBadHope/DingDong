using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen;

internal sealed partial class ContinentalSurfaceSurvey
{
	private const int SurveyAboveSurface = 280;
	private const int SurveyBelowSurface = 260;
	private const int SurfaceBandDepth = 72;
	private const int MaximumSpanGap = 48;
	private readonly int[] ground;
	private readonly ContinentalSurfaceKind[] kind;

	internal ContinentalSpan DesertSource { get; private set; }
	internal ContinentalSpan SnowSource { get; private set; }
	internal ContinentalSpan JungleSource { get; private set; }
	internal ContinentalSpan DesertTarget { get; private set; }
	internal ContinentalSpan SnowTarget { get; private set; }
	internal ContinentalSpan JungleTarget { get; private set; }
	internal ContinentalSpan ForestReserve { get; private set; }
	internal int ForestCenterX { get; private set; }
	internal long TilesInspected { get; private set; }
	internal string Failure { get; private set; }
	internal bool IsComplete => Failure == null;

	private ContinentalSurfaceSurvey(int width)
	{
		ground = new int[width];
		kind = new ContinentalSurfaceKind[width];
		Array.Fill(ground, -1);
	}

	internal static ContinentalSurfaceSurvey Create()
	{
		var result = new ContinentalSurfaceSurvey(Main.maxTilesX);
		if (Main.remixWorld)
		{
			result.Failure = "remix-surface-unsupported";
			return result;
		}
		result.ScanSurfaceBand();
		result.DesertSource = result.FindSpan(ContinentalSurfaceKind.Desert);
		result.SnowSource = result.FindSpan(ContinentalSurfaceKind.Snow);
		result.JungleSource = result.FindSpan(ContinentalSurfaceKind.Jungle);
		if (!result.DesertSource.IsValid || !result.SnowSource.IsValid || !result.JungleSource.IsValid)
		{
			result.Failure = $"source-missing:desert={result.DesertSource}:snow={result.SnowSource}:jungle={result.JungleSource}";
			return result;
		}
		// Spawn Point has not run yet at the Full Desert anchor. The geometric center
		// is current-world state and cannot leak a previous world's spawn coordinate.
		result.ForestCenterX = Main.maxTilesX / 2;
		int forestHalf = Math.Max(900, Main.maxTilesX / 12);
		result.ForestReserve = result.ClampSpan(result.ForestCenterX - forestHalf, result.ForestCenterX + forestHalf);
		result.DesertTarget = result.TargetAround(result.DesertSource, 0.15);
		result.SnowTarget = result.TargetAround(result.SnowSource, 0.15);
		result.JungleTarget = result.TargetAround(result.JungleSource, 0.18);
		return result;
	}

	internal int GroundAt(int x) => x >= 0 && x < ground.Length ? ground[x] : -1;

	private void ScanSurfaceBand()
	{
		int top = Math.Clamp((int)Main.worldSurface - SurveyAboveSurface, 40, Main.maxTilesY - 200);
		int bottom = Math.Clamp((int)Main.worldSurface + SurveyBelowSurface, top + 1, Main.maxTilesY - 80);
		for (int x = 20; x < Main.maxTilesX - 20; x++)
		{
			int y = FindGround(x, top, bottom);
			ground[x] = y;
			if (y >= 0)
				kind[x] = Classify(x, y, Math.Min(bottom, y + SurfaceBandDepth));
		}
	}

	private int FindGround(int x, int top, int bottom)
	{
		for (int y = top; y < bottom; y++)
		{
			TilesInspected++;
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				return y;
		}
		return -1;
	}

	private ContinentalSurfaceKind Classify(int x, int top, int bottom)
	{
		int desert = 0, snow = 0, jungle = 0, evil = 0;
		for (int y = top; y < bottom; y++)
		{
			TilesInspected++;
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile)
				continue;
			ushort type = tile.TileType;
			desert += IsDesert(type) ? 1 : 0;
			snow += IsSnow(type) ? 1 : 0;
			jungle += IsJungle(type) ? 1 : 0;
			evil += IsEvil(type) ? 1 : 0;
		}
		if (evil >= 4) return ContinentalSurfaceKind.Evil;
		if (desert >= 5) return ContinentalSurfaceKind.Desert;
		if (snow >= 5) return ContinentalSurfaceKind.Snow;
		if (jungle >= 5) return ContinentalSurfaceKind.Jungle;
		return ContinentalSurfaceKind.Forest;
	}

	private static bool IsDesert(ushort t) => t is TileID.Sand or TileID.HardenedSand or TileID.Sandstone;
	private static bool IsSnow(ushort t) => t is TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce or TileID.Slush;
	private static bool IsJungle(ushort t) => t is TileID.Mud or TileID.JungleGrass;
	private static bool IsEvil(ushort t) => t is TileID.Ebonstone or TileID.Crimstone or TileID.CorruptGrass or TileID.CrimsonGrass or TileID.CorruptIce or TileID.FleshIce or TileID.Ebonsand or TileID.Crimsand;
}
