using System;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Forces every immutable worldgen manifest to initialize during mod loading.
/// This turns missing catalog entries, unreviewed compact anchors and stale
/// housing sets into a short load-gate failure instead of a late worldgen crash.
/// </summary>
public sealed class WorldgenManifestCompatibilityContractSystem : ModSystem
{
	public override void PostSetupContent()
	{
		try
		{
			if (AuthoredPresetCatalog.All.Count != 105)
				throw new InvalidOperationException($"catalog-count:{AuthoredPresetCatalog.All.Count}/105");
			if (!NpcAuthoredPresetManifest.IsCandidateComplete)
				throw new InvalidOperationException($"npc-candidates:{NpcAuthoredPresetManifest.CandidateCount}/61");
			if (!AuthoredCandidateDeploymentManifest.IsComplete)
				throw new InvalidOperationException($"deployment-profiles:{AuthoredCandidateDeploymentManifest.Count}/61");
			if (!NpcAuthoredPresetManifest.TryGetCandidate("FishermanNPC/Fisherman", out AuthoredPreset fishermanPreset)
				|| !AuthoredCandidateDeploymentManifest.TryGet("FishermanNPC/Fisherman", out AuthoredDeploymentProfile fishermanProfile))
			{
				throw new InvalidOperationException("fisherman-profile:missing");
			}
			if (fishermanPreset.Id != "Gnorp_MediumOceanHouse"
				|| fishermanPreset.PlacementMode != AuthoredPlacementMode.HarborScene
				|| fishermanProfile.SourceAnchor != DeploymentSourceAnchor.AuthoredSceneBounds
				|| fishermanProfile.TargetAnchor != DeploymentTargetAnchor.SurfaceTerrainEnvelope)
			{
				throw new InvalidOperationException(
					$"fisherman-profile:{fishermanPreset.Id}:{fishermanPreset.PlacementMode}:{fishermanProfile.SourceAnchor}:{fishermanProfile.TargetAnchor}");
			}

			// Accessing both repair sets executes their mapping/count invariants now.
			if (!AuthoredHousingPreflightManifest.RequiresFurnitureSupplement("Aegis8_SteampunkOrb"))
				throw new InvalidOperationException("housing-preflight:current-repair-set");
			if (AuthoredHousingServerBaselineManifest.RequiresRepair("Aegis8_SteampunkOrb"))
				throw new InvalidOperationException("housing-baseline:unexpected-active-repair");

			Mod.Logger.Info(
				"DINGDONG_WORLDGEN_MANIFEST_CONTRACT|verdict=PASS|catalog=105|candidates=61|deployments=61|fisherman=Gnorp_MediumOceanHouse:HarborScene:AuthoredSceneBounds:SurfaceTerrainEnvelope|housingSets=PASS");
		}
		catch (Exception exception)
		{
			Mod.Logger.Error($"DINGDONG_WORLDGEN_MANIFEST_CONTRACT|verdict=FAIL|failure={exception.Message}");
			throw new InvalidOperationException("DingDong worldgen manifest load gate failed.", exception);
		}
	}
}
