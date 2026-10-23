using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static List<Rectangle> UpgradeVerifiedDerivedPyramid(Mod mod, string snapshotPath,
		JsonElement snapshot, List<Rectangle> original)
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_REPLAY_DERIVED_PYRAMID") != "1") return original;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Derived guard migration requires isolated replay.");
		using var proof = JsonDocument.Parse(File.ReadAllText(Main.ActiveWorldFileData.Path + ".derived-pyramid-proof.json"));
		var p = proof.RootElement;
		string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
		if (p.GetProperty("schema").GetInt32() != 1 || p.GetProperty("owner").GetString() != "DingDongBiomeHomes" ||
			p.GetProperty("seed").GetInt32() != Main.ActiveWorldFileData.Seed ||
			p.GetProperty("width").GetInt32() != Main.maxTilesX || p.GetProperty("height").GetInt32() != Main.maxTilesY ||
			p.GetProperty("generatorArtifact").GetString() != snapshot.GetProperty("artifact").GetString() ||
			Hash(snapshotPath) != p.GetProperty("snapshotSha256").GetString() ||
			Hash(Main.ActiveWorldFileData.Path) != p.GetProperty("worldSha256").GetString())
			throw new InvalidOperationException("Legacy derived guard evidence does not match this exact fixture.");
		int ordinal = p.GetProperty("regionIndex").GetInt32();
		var r = p.GetProperty("legacyEnvelope");
		Rectangle legacy = new(r.GetProperty("X").GetInt32(), r.GetProperty("Y").GetInt32(),
			r.GetProperty("Width").GetInt32(), r.GetProperty("Height").GetInt32());
		if (original.Count != p.GetProperty("regionCount").GetInt32() || ordinal < 0 || ordinal >= original.Count ||
			original[ordinal] != legacy || original.Count(v => v == legacy) != 1)
			throw new InvalidOperationException("Derived guard ordinal or envelope mismatch; foreign reservations must not be removed.");
		var shape = NativePyramidProtection.DescribeFromVerifiedEnvelope(legacy);
		if (shape.SourceCellCount != p.GetProperty("sourceCellCount").GetInt32() ||
			shape.GateCellCount != p.GetProperty("gateCellCount").GetInt32())
			throw new InvalidOperationException("Legacy pyramid source cells or legitimate gate changed.");
		var updated = original.Where((_, i) => i != ordinal).ToList();
		updated.AddRange(shape.Regions);
		if (!updated.Take(original.Count - 1).SequenceEqual(original.Where((_, i) => i != ordinal)))
			throw new InvalidOperationException("Foreign reservation sequence changed during derived guard replay.");
		mod.Logger.Info($"DINGDONG_DERIVED_GUARD_REPLAY|state=UPGRADED|ownLegacyGuards=1|foreignPreserved={original.Count - 1}|newOwnRegions={shape.Regions.Count}|pyramidCells={shape.SourceCellCount}|gateCells={shape.GateCellCount}|worldMutated=False");
		return updated;
	}
}
