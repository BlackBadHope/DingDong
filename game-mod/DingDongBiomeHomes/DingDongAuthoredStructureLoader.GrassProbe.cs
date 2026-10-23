using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	// Diagnose a saved mismatch in-place in RAM; restore all five tile planes.
	// This does not relax integrity or rewrite the saved world.
	internal static void ProbeGolfGrass(Mod mod)
	{
		if (!SettlementWorldState.TryGetResidence("Terraria/GoblinTinkerer", out var report) ||
			!NpcAuthoredPresetManifest.TryGetCandidate(report.NpcKey, out var preset))
			throw new InvalidOperationException("Workshop fixture missing.");
		StructureData source = GetOrLoad(mod, preset.ResourcePath);
		int mismatches = 0, nativeConversions = 0;
		for (int i = 0; i < source.Width * source.Height; i++)
		{
			SourceCell expected = ReadSourceCell(source, i);
			int x = report.Area.X + i / source.Height, y = report.Area.Y + i % source.Height;
			Tile actual = Main.tile[x, y];
			if (!expected.HasTile || expected.TileType != TileID.GolfGrass || !actual.HasTile || actual.TileType != TileID.Dirt) continue;
			mismatches++;
			Rectangle area = Rectangle.Intersect(new(x - 6, y - 6, 13, 13), new(1, 1, Main.maxTilesX - 2, Main.maxTilesY - 2));
			AuthoredTileSnapshot[] snapshot = new AuthoredTileSnapshot[area.Width * area.Height];
			for (int a = 0; a < area.Width; a++) for (int b = 0; b < area.Height; b++)
				snapshot[a * area.Height + b] = AuthoredTileSnapshot.Read(Main.tile[area.X + a, area.Y + b]);
			try
			{
				actual.TileType = TileID.GolfGrass;
				WorldGen.TileFrame(x, y, false, false);
				if (actual.HasTile && actual.TileType == TileID.Dirt) nativeConversions++;
				mod.Logger.Info($"DINGDONG_GOLF_GRASS_REPLAY|x={x}|y={y}|source=477|loaded=0|nativeReframe={actual.TileType}|hasTile={actual.HasTile}|liquid={actual.LiquidAmount}/{actual.LiquidType}|above={Main.tile[x,y-1].HasTile}/{Main.tile[x,y-1].TileType}|below={Main.tile[x,y+1].HasTile}/{Main.tile[x,y+1].TileType}");
			}
			finally
			{
				for (int a = 0; a < area.Width; a++) for (int b = 0; b < area.Height; b++)
					snapshot[a * area.Height + b].Restore(Main.tile[area.X + a, area.Y + b]);
			}
		}
		mod.Logger.Info($"DINGDONG_GOLF_GRASS_REPLAY|mismatches={mismatches}|nativeConversions={nativeConversions}|integrityUnchanged=True|saved=False");
		if (mismatches == 0) mod.Logger.Info("DINGDONG_GOLF_GRASS_REPLAY|result=NOT_REPRODUCED|notAnIntegrityPass=True");
	}
}
