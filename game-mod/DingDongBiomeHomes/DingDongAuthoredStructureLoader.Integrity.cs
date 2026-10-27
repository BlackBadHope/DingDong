using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal readonly record struct IntegrityReport(int OmittedTiles, int OmittedWalls,
		int MissingSolidConstruction, int MissingHouseWalls, string FirstFailure)
	{
		internal bool Passed => OmittedTiles + OmittedWalls + MissingSolidConstruction + MissingHouseWalls == 0;
	}
	internal static IntegrityReport InspectIntegrity(Mod mod, AuthoredPreset preset, Point origin, AuthoredDeploymentProfile profile)
	{
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
		int omittedTiles = 0, omittedWalls = 0, missingSolids = 0, missingWalls = 0;
		string first = "none";
		for (int i = 0; i < data.Width * data.Height; i++)
		{
			SourceCell source = ReadSourceCell(data, i);
			// Mowed lawn reverts to dirt like any grass once burnt or covered; it is
			// scenery, not masonry, so its loss must not fail the whole world.
			bool construction = source.HasTile && !IsNaturalTerrainTile(source.TileType) &&
				source.TileType is not (Terraria.ID.TileID.GolfGrass or Terraria.ID.TileID.GolfGrassHallowed);
			bool houseWall = source.WallType != 0 && source.WallType < Main.wallHouse.Length && Main.wallHouse[source.WallType];
			int x = origin.X + i / data.Height, y = origin.Y + i % data.Height;
			Tile actual = Main.tile[x, y];
			if (construction && !plan.WriteTiles[i]) omittedTiles++;
			if (houseWall && !plan.WriteWalls[i]) omittedWalls++;
			// Natural conversion, plants, doors and animation frames can legitimately
			// change. Solid authored masonry and housing walls cannot disappear.
			bool solid = construction && Main.tileSolid[source.TileType] && !Main.tileFrameImportant[source.TileType] &&
				!Terraria.ID.TileID.Sets.Falling[source.TileType];
			if (solid && (!actual.HasTile || actual.TileType != source.TileType))
			{
				missingSolids++;
				if (first == "none") first = $"tile:{x},{y}:expected={source.TileType}:actual={actual.HasTile}/{actual.TileType}";
			}
			if (houseWall && actual.WallType != source.WallType)
			{
				missingWalls++;
				if (first == "none") first = $"wall:{x},{y}:expected={source.WallType}:actual={actual.WallType}";
			}
		}
		return new(omittedTiles, omittedWalls, missingSolids, missingWalls, first);
	}
	internal static void RequireIntegrity(Mod mod, AuthoredPreset preset, Point origin, AuthoredDeploymentProfile profile, string stage)
	{
		IntegrityReport report = InspectIntegrity(mod, preset, origin, profile);
		string line = $"DINGDONG_SCENE_INTEGRITY|verdict={(report.Passed ? "PASS" : "FAIL")}|stage={stage}|preset={preset.Id}|origin={origin}|omittedTiles={report.OmittedTiles}|omittedWalls={report.OmittedWalls}|missingMasonry={report.MissingSolidConstruction}|missingWalls={report.MissingHouseWalls}|first={report.FirstFailure}";
		if (report.Passed) mod.Logger.Info(line);
		else { mod.Logger.Error(line); throw new InvalidOperationException(line); }
	}
}
