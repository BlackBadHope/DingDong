using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	internal static void BuildSyntheticResidenceRegistry(List<string> failures)
	{
		List<CandidateDeploymentSite> sites = new();
		int index = 0;
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!AuthoredCandidateDeploymentManifest.TryGet(identity.NpcKey, out AuthoredDeploymentProfile profile) || !NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset) || !AuthoredResidencePlacementManifest.TryGet(identity.NpcKey, out AuthoredResidencePlacementContract contract))
			{
				failures.Add($"residence-manifest:{identity.NpcKey}");
				continue;
			}

			Rectangle area = new(100 + index % 8 * 350, 100 + index / 8 * 120, preset.Width, preset.Height);
			sites.Add(new CandidateDeploymentSite(identity.NpcKey, profile.PresetId, identity.Biome, area, profile.ProtectedFootprint(area), area.Center, "quest-probe", Array.Empty<PersistentObjectCollision>(), contract.PreferredAltitudeBand, 0));
			index++;
		}

		if (sites.Count == NpcTownManifest.AuthoredCount)
			SettlementWorldState.CompleteCandidateGeneration(sites, sites.Count, sites.Count, sites.Count);
		Expect(failures, "residence-registry", SettlementWorldState.HasCompleteResidenceRegistry);
	}
}
