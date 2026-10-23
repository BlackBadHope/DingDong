using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria;
using Terraria.Map;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	// Read-only comparison; this diagnostic does not generate, frame or save tiles.
	internal static void ExportSceneAudit(Mod mod)
	{
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "SceneIntegrity", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
		Directory.CreateDirectory(directory);
		MapHelper.Initialize();
		Worldgen.NativeTerrainPalette palette = new();
		List<object> reports = new();
		foreach (var residence in SettlementWorldState.Residences)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out AuthoredPreset preset) ||
				!AuthoredCandidateDeploymentManifest.TryGet(residence.NpcKey, out var profile))
				throw new InvalidDataException("Unresolved residence: " + residence.NpcKey);
			StructureData data = GetOrLoad(mod, preset.ResourcePath);
			StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
			var integrity = InspectIntegrity(mod, preset, residence.Area.Location, profile);
			int omittedConstruction = 0, omittedNatural = 0, missingPlaced = 0, expectedPlaced = 0;
			int sourceLava = 0, actualLava = 0, unexpectedAuthoredLava = 0;
			List<object> missingExamples = new();
			for (int index = 0; index < data.Width * data.Height; index++)
			{
				SourceCell scratch = ReadSourceCell(data, index);
				Tile observed = Main.tile[residence.Area.X + index / data.Height, residence.Area.Y + index % data.Height];
				bool rawLava = scratch.LiquidAmount > 0 && scratch.LiquidType == 1;
				bool placedLava = observed.LiquidAmount > 0 && observed.LiquidType == 1;
				if (rawLava) sourceLava++;
				if (placedLava) actualLava++;
				if (placedLava && !rawLava && plan.WriteTiles[index]) unexpectedAuthoredLava++;
				if (!scratch.HasTile) continue;
				if (!plan.WriteTiles[index])
				{
					if (IsNaturalTerrainTile(scratch.TileType)) omittedNatural++;
					else omittedConstruction++;
				}
				else
				{
					expectedPlaced++;
					Tile actual = Main.tile[residence.Area.X + index / data.Height, residence.Area.Y + index % data.Height];
					if (!actual.HasTile || actual.TileType != scratch.TileType)
					{
						missingPlaced++;
						if (missingExamples.Count < 24) missingExamples.Add(new { x = index / data.Height, y = index % data.Height,
							expected = scratch.TileType, actual = actual.TileType, actual.HasTile });
					}
				}
			}
			var housing = AuthoredSettlementBuilder.ProbeHousing(residence.Area);
			var housingFailures = housing.ValidRooms == 0 ? AuthoredSettlementBuilder.ExplainHousingFailures(residence.Area) : null;
			reports.Add(new { residence.ReviewId, residence.NpcKey, preset.Id, x = residence.Area.X, y = residence.Area.Y,
				data.Width, data.Height, omittedConstruction, omittedNatural, expectedPlaced, missingPlaced,
				sourceLava, actualLava, unexpectedAuthoredLava, housing, housingFailures, missingExamples,
				focus = plan.FocusBounds?.ToString(), plan.DiscardedMutationCellCount, integrity });
			WriteSceneComparison(Path.Combine(directory, $"{residence.ReviewId:D2}-{preset.Id}.bmp"), data, plan, residence, palette);
		}
		File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new {
			world = Main.ActiveWorldFileData.Path, worldMutated = false,
			panels = "left: raw source; middle: write plan on empty background; right: saved-world tiles; one pixel per tile; diagnostic palette without paint/textures",
			reports }, new JsonSerializerOptions { WriteIndented = true }));
		Console.WriteLine($"DINGDONG_SCENE_AUDIT|count={reports.Count}|directory={directory}|worldMutated=False");
	}
	internal static void ExportSceneComparison(Mod mod, SettlementResidenceReport residence, string path)
	{
		NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out AuthoredPreset preset);
		AuthoredCandidateDeploymentManifest.TryGet(residence.NpcKey, out var profile);
		StructureData data = GetOrLoad(mod, preset.ResourcePath);
		MapHelper.Initialize();
		WriteSceneComparison(path, data, GetWritePlan(data, preset.ResourcePath, profile), residence, new Worldgen.NativeTerrainPalette());
	}

	private static void WriteSceneComparison(string path, StructureData data, StructureWritePlan plan,
		SettlementResidenceReport residence, Worldgen.NativeTerrainPalette palette)
	{
		int w = data.Width * 3 + 8, h = data.Height, stride = (w * 3 + 3) & ~3;
		using BinaryWriter writer = new(File.Create(path));
		writer.Write((ushort)0x4D42); writer.Write(54 + stride * h); writer.Write(0); writer.Write(54);
		writer.Write(40); writer.Write(w); writer.Write(-h); writer.Write((ushort)1); writer.Write((ushort)24);
		writer.Write(0); writer.Write(stride * h); writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
		byte[] row = new byte[stride];
		for (int y = 0; y < h; y++)
		{
			Array.Clear(row);
			for (int x = 0; x < data.Width; x++)
			{
				int index = x * data.Height + y;
				SourceCell raw = ReadSourceCell(data, index);
				PutColor(row, x, palette.At(raw.HasTile, raw.TileType, raw.WallType, raw.LiquidAmount, raw.LiquidType, 0));
				PutColor(row, x + data.Width + 4, palette.At(raw.HasTile && plan.WriteTiles[index], raw.TileType,
					plan.WriteWalls[index] ? raw.WallType : (ushort)0, plan.WriteTiles[index] ? raw.LiquidAmount : (byte)0, raw.LiquidType, 0));
				PutColor(row, x + 2 * (data.Width + 4), palette.At(residence.Area.X + x, residence.Area.Y + y));
			}
			writer.Write(row);
		}
	}
	private static void PutColor(byte[] row, int x, Color color)
	{
		row[x * 3] = color.B; row[x * 3 + 1] = color.G; row[x * 3 + 2] = color.R;
	}
}
