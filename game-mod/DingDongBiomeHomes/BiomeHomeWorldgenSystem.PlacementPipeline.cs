using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	internal void RunWorldCompilerPass(GenerationProgress progress,
		GameConfiguration configuration) => GenerateHomes(progress, configuration);

	private void GenerateHomes(GenerationProgress progress, GameConfiguration configuration)
	{
		try
		{
			using var cache = DingDongAuthoredStructureLoader.UseTransientCaches();
			GenerateHomesCore(progress, configuration);
		}
		finally { DingDongAuthoredStructureLoader.ReportCacheRelease(Mod, "authored-residences-finally"); }
	}

	private void GenerateHomesCore(GenerationProgress progress, GameConfiguration configuration)
	{
		Worldgen.NativeWorldgenProbeCheckpoint.Save(Mod);
		Worldgen.NativePyramidProtection.Reserve(Mod);
		ResidenceReservationDiagnostics.Capture(Mod);
		if (ResidenceReservationDiagnostics.RunRequestedPreflight(Mod)) return;
		progress.Message = "Planning 61 authored biome residences";
		string seedProfile = DescribeSeedProfile();
		SettlementWorldState.BeginGeneration(seedProfile, NpcTownManifest.AuthoredCount,
			housesPerDistrict: 1);
		Mod.Logger.Info($"DINGDONG_WORLDGEN_BEGIN|mode=technical-candidate-admission|seed={Main.ActiveWorldFileData?.Seed}|seedText={Main.ActiveWorldFileData?.SeedText}|size={Main.maxTilesX}x{Main.maxTilesY}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}|candidates={NpcAuthoredPresetManifest.CandidateCount}/61|visualApproved={NpcAuthoredPresetManifest.Count}/61");
		if (!CandidateDeploymentSitePlanner.TryPlanSimultaneous(Mod,
			AuthoredCandidateDeploymentManifest.All, out List<CandidateDeploymentSite> sites,
			out string planningFailure))
		{
			Mod.Logger.Error($"DINGDONG_PLANNER_TRACE|verdict=FAIL|{CandidateDeploymentSitePlanner.LastSolverTrace}");
			SettlementWorldState.AbortGeneration();
			if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive)
				throw new InvalidOperationException(
					$"Compiler residence planning failed before mutation: {planningFailure}.");
			progress.Message = "DingDong residences skipped safely";
			Mod.Logger.Warn($"DINGDONG_WORLDGEN_END|verdict=DEGRADED|phase=simultaneous-plan|failure={planningFailure}|mutated=0|planned=0/61|worldgenContinues=True");
			return;
		}

		Mod.Logger.Info($"DINGDONG_PLANNER_TRACE|verdict=PASS|{CandidateDeploymentSitePlanner.LastSolverTrace}");
		if (sites.Count != NpcTownManifest.AuthoredCount)
			throw new InvalidOperationException(
				$"DingDong simultaneous layout is incomplete: {sites.Count}/{NpcTownManifest.AuthoredCount}.");
		int layoutIndex = 0;
		foreach (CandidateDeploymentSite site in sites)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(site.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException(
					$"DingDong layout report missing preset: {site.NpcKey}.");
			layoutIndex++;
			Mod.Logger.Info(AuthoredResidenceLayoutPolicy.FormatSiteLine(layoutIndex,
				site, preset, sites));
		}

		if (!PersistentChestRelocator.TryPlanAll(sites,
			out List<ChestRelocationPlan> relocationPlans, out string relocationFailure))
			throw new InvalidOperationException(
				$"DingDong lossless collision batch plan failed: {relocationFailure}.");
		// Capture foreign regions before adding our own spacing reservations.
		var foreignGuard = Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive
			? null : Worldgen.ForeignStructureIntegrityGuard.Capture(Mod);
		if (!AuthoredResidenceStructureReservation.TryReserveAll(sites,
			out string reservationFailure))
		{
			SettlementWorldState.AbortGeneration();
			progress.Message = "DingDong residences withheld by StructureMap";
			bool compiler = Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive;
			Mod.Logger.Error($"DINGDONG_WORLDGEN_END|verdict=WITHHELD|phase=structure-reservation|failure={reservationFailure}|reserved=0/61|mutated=0|worldgenContinues={!compiler}");
			if (compiler)
				throw new InvalidOperationException(
					$"Compiler residence reservation failed before mutation: {reservationFailure}.");
			return;
		}
		Mod.Logger.Info("DINGDONG_WORLDGEN_RESERVATION|verdict=PASS|reserved=61/61|beforeMutation=True");

		int relocatedChests = 0;
		relocatedChests = ApplyRelocationsAndPlaceResidences(progress, sites,
			relocationPlans, relocatedChests, out int structuresPlaced,
			out int housingValid, out int roomsDiscovered, out HashSet<string> uniquePresets);
		// Owner item 6: metro coast to coast and a hellevator beside the sanctuary,
		// carved around every reserved structure; the foreign guard below repairs
		// anything a provider did not register.
		progress.Message = "DingDong residence transit: metro and hellevator";
		Worldgen.ResidenceTransitBuilder.Build(Mod);
		foreignGuard?.RepairAndValidate(Mod);
		Worldgen.NativePyramidProtection.Validate(Mod, "authored-residences");
		Mod.Logger.Info($"DINGDONG_WORLDGEN_END|verdict=PASS|mode=technical-candidate-admission|simultaneousLayout=True|structures={structuresPlaced}/61|uniquePresets={uniquePresets.Count}/61|housing={housingValid}/61|rooms={roomsDiscovered}|relocatedChests={relocatedChests}|unresolvedPersistentObjectOverlaps=0|visualApproved={NpcAuthoredPresetManifest.Count}/61|distributionAccepted=True|playerVisualApproved=False|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
	}
}
