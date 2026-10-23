using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class DingDongTerrainAutoProbeSystem : ModSystem
{
	private bool ran;
	private bool waitingForSettling;
	public override void Load()
	{
		if (Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") is "@settled-quality" or "@grass-contracts" &&
			Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") == "1")
			WorldFile.OnWorldLoad += OnNativeWorldLoaded;
	}
	public override void Unload() => WorldFile.OnWorldLoad -= OnNativeWorldLoaded;
	private void OnNativeWorldLoaded()
	{
		if (!waitingForSettling) return;
		waitingForSettling = false;
		try
		{
			// WorldFile raises this only after its successful native load and liquid settling,
			// unlike SystemLoader.PostWorldLoad; no player or console-input loop is required.
			if (WorldGen.gen) throw new InvalidOperationException("Native load settling is still active.");
			using var cache = DingDongAuthoredStructureLoader.UseTransientCaches();
			string probe = Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC");
			if (probe == "@grass-contracts") DingDongAuthoredStructureLoader.ProbeGolfGrass(Mod);
			else Worldgen.NativeGeometryQualityProbe.Run(Mod);
			Console.WriteLine("DINGDONG_TERRAIN_PROBE_END|npc=" + probe);
		}
		catch (Exception ex)
		{
			Mod.Logger.Error("DINGDONG_TERRAIN_PROBE_ERROR", ex);
			Console.WriteLine("DINGDONG_TERRAIN_PROBE_ERROR|" + ex.GetType().Name);
		}
	}
	public override void PostWorldLoad()
	{
		string key = Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC");
		if (ran || !Main.dedServ || string.IsNullOrEmpty(key) ||
			Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!Path.GetFileName(Main.ActiveWorldFileData?.Path ?? "").StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal)) return;
		ran = true;
		try
		{
			if (key is "@settled-quality" or "@grass-contracts")
			{
				if (Environment.GetEnvironmentVariable("DINGDONG_QUALITY_BASELINE") == "1")
				{
					using var baselineCache = DingDongAuthoredStructureLoader.UseTransientCaches();
					DingDongAuthoredStructureLoader.ExportSceneAudit(Mod);
					Mod.Logger.Info("DINGDONG_QUALITY_BASELINE|phase=before-native-load-settling");
				}
				waitingForSettling = true;
				Mod.Logger.Info("DINGDONG_QUALITY_WAIT|phase=post-load|awaitingNativeLoadEvent=True");
				return;
			}
			if (key == "@adaptation")
			{
				Worldgen.WorldgenAdaptationProbe.Run(Mod);
				Console.WriteLine("DINGDONG_TERRAIN_PROBE_END|npc=" + key);
				return;
			}
			using var cache = DingDongAuthoredStructureLoader.UseTransientCaches();
			if (key == "@underworld-contracts") Worldgen.NativeUnderworldProbe.Run(Mod);
			else if (key == "@residence-wire") SettlementWorldState.RunWireProbe(Mod);
			else if (key == "@doctrine-contracts") NpcResidenceWorldSystem.RunDoctrineProbe(Mod);
			else if (key == "@map-export") Worldgen.NativeTerrainRaster.Export();
			else if (key == "@terrain-replay") Worldgen.NativeBigRcTerrainReplay.Run(Mod);
			else if (key == "@scene-audit") DingDongAuthoredStructureLoader.ExportSceneAudit(Mod);
			else if (key == "@scene-probe") AuthoredSceneProbe.Run(Mod);
			else if (key == "@write-safety") Worldgen.ResidenceWriteSafetyProbe.Run(Mod);
			else if (key == "@liquid-safety") DingDongAuthoredStructureLoader.ProbeLiquidSafety(Mod);
			else if (key == "@joint-liquid") DingDongAuthoredStructureLoader.ProbeJointLiquidSafety(Mod);
			else if (key == "@scene-repair") ResidenceSceneRepairProbe.Run(Mod);
			else if (key == "@map-and-simultaneous")
			{
				Worldgen.NativeSlopeBoundarySystem.Probe();
				Worldgen.NativeTerrainRaster.Export();
				CandidateDeploymentSitePlanner.ReplayReservationPlan(Mod);
			}
			else if (key == "@topaz-anchor")
			{
				var roof = Worldgen.Exact66Adapters.SotsGemPlacement.FindNativeRoof();
				for (int x = roof.X - 2; x < roof.X + 2; x++)
					if (!Main.tile[x, roof.Y].HasTile || Main.tile[x, roof.Y].TileType != Terraria.ID.TileID.LihzahrdBrick)
						throw new InvalidOperationException("Native temple roof is not physically four tiles wide.");
				Mod.Logger.Info($"DINGDONG_NATIVE_TOPAZ_PROBE|verdict=PASS|roof={roof}|tiles=4|placement=False");
			}
			else if (key == "@simultaneous") CandidateDeploymentSitePlanner.ReplayReservationPlan(Mod);
			else if (key == "@candidate-domains") CandidateDeploymentSitePlanner.ReplayReservationPlan(Mod, domainsOnly: true);
			else if (key == "@place-residences") CandidateDeploymentSitePlanner.ReplayReservationPlan(Mod, place: true);
			else if (key == "@acceptance")
			{
				Worldgen.NativeWorldInventory.Export();
				ResidenceAcceptanceProbe.Run(Mod);
				Worldgen.NativeTerrainRaster.Export();
			}
			else
			{
				var profile = AuthoredCandidateDeploymentManifest.All.Single(p => p.NpcKey == key);
				CandidateDeploymentSitePlanner.DiagnoseTerrain(Mod, profile);
			}
			Console.WriteLine("DINGDONG_TERRAIN_PROBE_END|npc=" + key);
		}
		catch (Exception ex)
		{
			Mod.Logger.Error("DINGDONG_TERRAIN_PROBE_ERROR", ex);
			Console.WriteLine("DINGDONG_TERRAIN_PROBE_ERROR|" + ex.GetType().Name);
		}
	}
	public override void OnWorldUnload() { ran = false; waitingForSettling = false; }
}
