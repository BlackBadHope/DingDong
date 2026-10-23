using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class DingDongAuthoredStructureLoader
{
	private sealed record LiquidProjectionScene(string Npc, Point Origin, StructureData Data,
		StructureWritePlan Plan, LiquidSafetyEnvelope Envelope)
	{
		internal Rectangle Area => new(Origin.X, Origin.Y, Data.Width, Data.Height);
	}

	// Immutable logical overlay: no tile, liquid, wall or furniture is written.
	// All selected scenes participate at once, so one import cannot hide a dam
	// removed by another. Buckets bound overlay lookup, not fluid reachability.
	private sealed class JointLiquidProjection
	{
		private const int BucketWidth = 128;
		internal readonly List<LiquidProjectionScene> Scenes = new();
		private readonly Dictionary<int, List<LiquidProjectionScene>> columns = new();
		internal int HighestLavaY { get; private set; } = int.MaxValue;
		internal long HeadInventoryReads { get; private set; }
		internal long RoomFilterReads { get; private set; }

		internal JointLiquidProjection(Mod mod, IReadOnlyList<(string Npc, Point Origin)> placements)
		{
			if (placements.Count != 61 || placements.Select(p => p.Npc).Distinct(StringComparer.Ordinal).Count() != 61)
				throw new InvalidOperationException("Joint liquid projection requires 61 distinct residences.");
			foreach (var placement in placements)
			{
				if (!NpcAuthoredPresetManifest.TryGetCandidate(placement.Npc, out var preset) ||
					!AuthoredCandidateDeploymentManifest.TryGet(placement.Npc, out var profile))
					throw new InvalidOperationException("Missing joint liquid source: " + placement.Npc);
				StructureData data = GetOrLoad(mod, preset.ResourcePath);
				StructureWritePlan plan = GetWritePlan(data, preset.ResourcePath, profile);
				LiquidSafetyEnvelope envelope = liquidSafetyEnvelopes.GetValue(plan, _ => BuildLiquidSafetyEnvelope(data, plan));
				var scene = new LiquidProjectionScene(placement.Npc, placement.Origin, data, plan, envelope);
				if (!new Rectangle(1, 1, Main.maxTilesX - 2, Main.maxTilesY - 2).Contains(scene.Area))
					throw new InvalidOperationException("Joint source canvas outside world: " + placement.Npc);
				Rectangle mutation = plan.MutationBounds; mutation.Offset(placement.Origin);
				foreach (var previous in Scenes)
				{
					Rectangle previousMutation = previous.Plan.MutationBounds; previousMutation.Offset(previous.Origin);
					if (mutation.Intersects(previousMutation))
						throw new InvalidOperationException("Joint projection requires disjoint mutation bounds: " + previous.Npc + "/" + placement.Npc);
				}
				Scenes.Add(scene);
				for (int bucket = scene.Area.Left / BucketWidth; bucket <= (scene.Area.Right - 1) / BucketWidth; bucket++)
				{
					if (!columns.TryGetValue(bucket, out var members)) columns.Add(bucket, members = new());
					members.Add(scene);
				}
				for (int i = 0; i < envelope.SourceFlags.Length; i++)
					if (plan.WriteTiles[i] && (envelope.SourceFlags[i] & LiquidIngressReachability.Lava) != 0)
						HighestLavaY = Math.Min(HighestLavaY, placement.Origin.Y + i % data.Height);
			}
			// Stop at the first native lava row, or at the already known authored
			// head. Overwritten or solid-contained native lava may overestimate
			// the head conservatively; a new authored source must never be omitted.
			for (int y = 1; y < Main.maxTilesY - 1 && y < HighestLavaY; y++)
			for (int x = 1; x < Main.maxTilesX - 1; x++)
			{
				HeadInventoryReads++;
				Tile tile = Main.tile[x, y];
				if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Lava) continue;
				HighestLavaY = y;
				break;
			}
		}

		internal byte Read(int index)
		{
			int x = index / Main.maxTilesY, y = index % Main.maxTilesY;
			if (x < 1 || y < 1 || x >= Main.maxTilesX - 1 || y >= Main.maxTilesY - 1) return 0;
			if (columns.TryGetValue(x / BucketWidth, out var members))
				foreach (var scene in members)
					if (scene.Area.Contains(x, y))
					{
						int local = (x - scene.Origin.X) * scene.Data.Height + y - scene.Origin.Y;
						if (scene.Plan.WriteTiles[local])
							return (byte)(scene.Envelope.SourceFlags[local] & (LiquidIngressReachability.Open | LiquidIngressReachability.Lava));
					}
			Tile tile = Main.tile[x, y];
			byte flags = !tile.HasTile || tile.IsActuated || LiquidCanPass(tile.TileType) ? LiquidIngressReachability.Open : (byte)0;
			if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava) flags |= LiquidIngressReachability.Lava;
			return flags;
		}

		internal List<int> Rooms(LiquidProjectionScene scene)
		{
			var result = new List<int>();
			for (int i = 0; i < scene.Envelope.SourceFlags.Length; i++)
			{
				if ((scene.Envelope.SourceFlags[i] & LiquidIngressReachability.Room) == 0) continue;
				int index = (scene.Origin.X + i / scene.Data.Height) * Main.maxTilesY + scene.Origin.Y + i % scene.Data.Height;
				RoomFilterReads++;
				if ((Read(index) & LiquidIngressReachability.Open) != 0) result.Add(index);
			}
			return result;
		}
	}

	internal static void ProbeJointLiquidSafety(Mod mod)
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") != "@joint-liquid" ||
			!Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Joint liquid diagnostic requires an isolated copy.");
		var projection = new JointLiquidProjection(mod, SettlementWorldState.Residences.Select(r => (r.NpcKey, r.Area.Location)).ToArray());
		const int readBudget = 262144;
		var reports = new List<object>();
		int safe = 0, threats = 0, unknown = 0;
		foreach (var scene in projection.Scenes)
		{
			var rooms = projection.Rooms(scene);
			var result = ExternalLiquidIngress.Query(Main.maxTilesX, Main.maxTilesY, rooms,
				projection.HighestLavaY, projection.Read, readBudget);
			if (result.Verdict == ExternalLiquidIngress.Decision.Safe) safe++;
			else if (result.Verdict == ExternalLiquidIngress.Decision.Threat) threats++;
			else unknown++;
			reports.Add(new { npc = scene.Npc, verdict = result.Verdict.ToString(), graphReads = result.Reads,
				roomCells = rooms.Count, threatX = result.ThreatIndex < 0 ? -1 : result.ThreatIndex / Main.maxTilesY,
				threatY = result.ThreatIndex < 0 ? -1 : result.ThreatIndex % Main.maxTilesY });
		}
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "JointLiquid");
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json");
		File.WriteAllText(path, JsonSerializer.Serialize(new { seed = Main.ActiveWorldFileData.Seed,
			width = Main.maxTilesX, height = Main.maxTilesY, conservativeHighestLavaY = projection.HighestLavaY,
			headInventoryReads = projection.HeadInventoryReads, roomFilterReads = projection.RoomFilterReads, perSceneGraphReadBudget = readBudget,
			safe, threats, unknown, houses = reports, worldMutated = false, admissionInstalled = false,
			model = "head-constrained reachability over virtual joint re-import; not saved furniture survival or finite-volume native fluid simulation" }));
		mod.Logger.Info($"DINGDONG_JOINT_LIQUID|state=EXPORTED|safe={safe}|threats={threats}|unknown={unknown}|worldMutated=False|admissionInstalled=False|path={path}");
	}
}
