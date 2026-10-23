using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using DingDongBiomeHomes.Fishing.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.IO;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Production geometry functions on an exact paired pre-residence test COPY.
// The ordinary world-load path never invokes this mutation.
internal static class NativeBigRcTerrainReplay
{
	internal static void Run(Mod mod)
	{
		string path = Main.ActiveWorldFileData?.Path ?? "";
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") != "@terrain-replay" ||
			!Path.GetFileName(path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal) ||
			!Path.GetFullPath(path).StartsWith(Path.GetFullPath(Main.SavePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
			SettlementWorldState.StructuresBuilt != 0 || !ContinentalWorldDimensions.IsContinental)
			throw new InvalidOperationException("Terrain replay requires an isolated x10 pre-residence fixture copy.");
		byte[] reservations = File.ReadAllBytes(path + ".residence-reservations.json");
		using var proof = JsonDocument.Parse(File.ReadAllBytes(path + ".replay-proof.json"));
		foreach (var (name, bytes) in new[] {
			("worldSha256", File.ReadAllBytes(path)), ("twldSha256", File.ReadAllBytes(Path.ChangeExtension(path,"twld"))),
			("snapshotSha256", reservations) })
			if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), proof.RootElement.GetProperty(name).GetString(), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Terrain replay fixture identity changed: " + name);
		using var snapshot = JsonDocument.Parse(reservations);
		JsonElement root = snapshot.RootElement;
		if (root.GetProperty("seed").GetInt32() != Main.ActiveWorldFileData.Seed ||
			root.GetProperty("width").GetInt32() != Main.maxTilesX || root.GetProperty("height").GetInt32() != Main.maxTilesY)
			throw new InvalidOperationException("Terrain reservation snapshot belongs to another world.");
		StructureMap original = GenVars.structures;
		bool wasGenerating = WorldGen.gen;
		try
		{
			GenVars.structures = new StructureMap();
			foreach (JsonElement r in root.GetProperty("regions").EnumerateArray())
				GenVars.structures.AddProtectedStructure(new(r.GetProperty("X").GetInt32(), r.GetProperty("Y").GetInt32(), r.GetProperty("Width").GetInt32(), r.GetProperty("Height").GetInt32()), 0);
			var integrity = ForeignStructureIntegrityGuard.Capture(mod);
			MapHelper.Initialize();
			string directory = Path.Combine(Main.SavePath,"DingDongDiagnostics","TerrainReplay");
			Directory.CreateDirectory(directory);
			NativeTerrainRaster.ExportRegion(Path.Combine(directory,"coast-right-before.bmp"),new(Main.maxTilesX-6000,700,6000,2300),3);
			WorldGen.gen = true;
			FishingWaterWorldSystem.ReplayOceanBedForProbe();
			if (!NativeSanctuaryPlacementSystem.ValidateGeometry(mod))
				throw new InvalidOperationException("Legacy Sanctuary physical geometry changed.");
			integrity.Validate(mod);
			NativeTerrainRaster.ExportRegion(Path.Combine(directory,"coast-right.bmp"),new(Main.maxTilesX-6000,700,6000,2300),3);
			NativeTerrainRaster.ExportRegion(Path.Combine(directory,"underworld.bmp"),new(0,Main.maxTilesY-800,Main.maxTilesX,800),4);
			WorldGen.gen = wasGenerating;
			WorldFile.SaveWorld(useCloudSaving:false,resetTime:false);
			mod.Logger.Info("DINGDONG_TERRAIN_REPLAY|verdict=PASS|providerReservationsChanged=0|saved=True|userSavesUsed=False|fullGeneration=False");
		}
		finally { GenVars.structures = original; WorldGen.gen = wasGenerating; }
	}
}
