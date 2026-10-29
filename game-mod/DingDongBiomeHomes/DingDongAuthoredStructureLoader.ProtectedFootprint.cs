using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	internal sealed record ProtectedFootprint(bool[] Tiles, bool[] Walls, int Height);

	// The exact cells the reviewed scene itself built: non-natural tiles and house walls the
	// write plan placed, minus the ground left to the host. Terrain, plants and anything grown
	// or placed later fall outside it. Deterministic from the preset, so client and server agree.
	internal static ProtectedFootprint BuildProtectedFootprint(Mod mod, SettlementResidenceReport residence)
	{
		if (!NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out AuthoredPreset preset) ||
			!AuthoredCandidateDeploymentManifest.TryGet(residence.NpcKey, out var profile))
			return null;
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		if (data.Width != residence.Area.Width || data.Height != residence.Area.Height)
			return null;
		StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
		SceneGroundDrop drop = BuildSceneGroundDrop(data, plan, profile);
		int count = data.Width * data.Height;
		bool[] tiles = new bool[count], walls = new bool[count];
		for (int index = 0; index < count; index++)
		{
			SourceCell cell = ReadSourceCell(data, index);
			tiles[index] = plan.WriteTiles[index] && !(drop != null && drop.Tiles[index]) && cell.HasTile && !IsNaturalTerrainTile(cell.TileType);
			walls[index] = plan.WriteWalls[index] && !(drop != null && drop.Walls[index]) && cell.WallType != WallID.None && !IsNaturalBackgroundWall(cell.WallType);
		}
		return new ProtectedFootprint(tiles, walls, data.Height);
	}
}
