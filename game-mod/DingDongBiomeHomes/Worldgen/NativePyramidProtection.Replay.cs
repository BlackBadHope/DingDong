using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal sealed partial class NativePyramidProtection
{
	// Restores only our validation capture. The caller already restored every
	// reservation; this path never adds/removes guards or writes SOTS static state.
	internal static IDisposable CaptureVerifiedPlacementReplay(Mod owner, JsonElement snapshot,
		byte[] snapshotBytes, IReadOnlyList<Rectangle> restoredRegions)
	{
		string worldPath = Main.ActiveWorldFileData?.Path ?? string.Empty;
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") != "@place-residences" ||
			!Path.GetFileName(worldPath).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal) ||
			!Path.GetFullPath(worldPath).StartsWith(Path.GetFullPath(Main.SavePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
			Environment.GetEnvironmentVariable("DINGDONG_REPLAY_DERIVED_PYRAMID") == "1" ||
			File.Exists(worldPath + ".derived-pyramid-proof.json"))
			throw new InvalidOperationException("Pyramid capture replay requires an isolated placement copy and cannot mix legacy migration.");
		if (captured || description != null || cells.Count != 0)
			throw new InvalidOperationException("Cannot replace an existing pyramid capture during replay.");
		using var proof = JsonDocument.Parse(File.ReadAllBytes(worldPath + ".replay-proof.json"));
		JsonElement receipt = proof.RootElement;
		if (receipt.GetProperty("schema").GetString() != "dingdong-residence-replay-pair-v1" ||
			!HashMatches(HashFile(worldPath), receipt, "worldSha256") ||
			!HashMatches(HashFile(Path.ChangeExtension(worldPath, ".twld")), receipt, "twldSha256") ||
			!HashMatches(Convert.ToHexString(SHA256.HashData(snapshotBytes)), receipt, "snapshotSha256"))
			throw new InvalidOperationException("Pyramid replay proof does not match the exact world, mod data and reservation snapshot.");
		if (snapshot.GetProperty("stage").GetString() != "before-authored-residences" ||
			snapshot.GetProperty("seed").GetInt32() != Main.ActiveWorldFileData.Seed ||
			snapshot.GetProperty("width").GetInt32() != Main.maxTilesX ||
			snapshot.GetProperty("height").GetInt32() != Main.maxTilesY)
			throw new InvalidOperationException("Pyramid replay snapshot stage or world identity changed.");
		JsonElement provenance = snapshot.GetProperty("derivedPyramidGuard");
		if (provenance.GetProperty("owner").GetString() != "DingDongBiomeHomes/NativePyramidProtection" ||
			provenance.GetProperty("shapeVersion").GetString() != "filled-row-hull-v1" ||
			provenance.GetProperty("uncoveredProtectedSourceCells").GetInt32() != 0)
			throw new InvalidOperationException("Pyramid replay requires current, fully covered DingDong-owned guard provenance.");
		Rectangle[] snapshotRegions = snapshot.GetProperty("regions").EnumerateArray().Select(ReadRectangle).ToArray();
		var field = typeof(StructureMap).GetField("_protectedStructures", BindingFlags.NonPublic | BindingFlags.Instance);
		if (!snapshotRegions.SequenceEqual(restoredRegions) ||
			field?.GetValue(GenVars.structures) is not List<Rectangle> actualRegions ||
			!snapshotRegions.SequenceEqual(actualRegions))
			throw new InvalidOperationException("Pyramid replay reservations differ from the paired snapshot.");
		Rectangle verifiedEnvelope = ReadRectangle(provenance.GetProperty("legacyEnvelope"));
		if (verifiedEnvelope.Width <= 0 || verifiedEnvelope.Height <= 0 ||
			Rectangle.Intersect(verifiedEnvelope, WorldBounds) != verifiedEnvelope ||
			!ModLoader.TryGetMod("SOTS", out Mod sots))
			throw new InvalidOperationException("Pyramid replay envelope or SOTS provider is unavailable.");
		GuardDescription verified = Scan(sots, verifiedEnvelope, out List<Cell> verifiedCells);
		Rectangle[] expectedRegions = provenance.GetProperty("regions").EnumerateArray().Select(ReadRectangle).ToArray();
		if (verified.LegacyEnvelope != verifiedEnvelope ||
			verified.SourceCellCount != provenance.GetProperty("sourceCellCount").GetInt32() ||
			verified.GateCellCount != provenance.GetProperty("gateCellCount").GetInt32() ||
			verified.GateBounds != ReadRectangle(provenance.GetProperty("gateBounds")) ||
			verified.ExtraSourceCells != provenance.GetProperty("extraSourceCells").GetInt32() ||
			verified.TotalProtectedSourceCells != provenance.GetProperty("totalProtectedSourceCells").GetInt32() ||
			verifiedCells.Count != verified.TotalProtectedSourceCells ||
			!verified.Regions.SequenceEqual(expectedRegions) ||
			expectedRegions.GroupBy(r => r).Any(group => actualRegions.Count(r => r == group.Key) < group.Count()))
			throw new InvalidOperationException("Pyramid replay cells, gate or exact guard regions no longer match their owned provenance.");
		var scope = new ReplayCaptureScope(verified);
		try
		{
			cells.AddRange(verifiedCells);
			description = verified;
			area = verified.LegacyEnvelope;
			captured = true;
			owner.Logger.Info($"DINGDONG_PYRAMID_REPLAY|state=CAPTURED|pairedSha256=True|cells={verified.SourceCellCount}|extraSourceCells={verified.ExtraSourceCells}|protectedSourceCells={cells.Count}|gateCells={verified.GateCellCount}|guardRegions={verified.Regions.Count}|addedGuards=0|foreignReservationsChanged=False|sotsOriginChanged=False|shape=filled-row-hull-v1");
			return scope;
		}
		catch { Reset(); throw; }
	}

	private static Rectangle ReadRectangle(JsonElement value) => new(value.GetProperty("X").GetInt32(),
		value.GetProperty("Y").GetInt32(), value.GetProperty("Width").GetInt32(), value.GetProperty("Height").GetInt32());
	private static string HashFile(string path)
	{
		using FileStream stream = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(stream));
	}
	private static bool HashMatches(string actual, JsonElement receipt, string name) =>
		string.Equals(actual, receipt.GetProperty(name).GetString(), StringComparison.OrdinalIgnoreCase);
	private sealed class ReplayCaptureScope(GuardDescription owned) : IDisposable
	{
		public void Dispose()
		{
			if (ReferenceEquals(description, owned)) Reset();
		}
	}
}
