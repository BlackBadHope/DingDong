using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	private static GenPass scheduledResidencePass;

	public override void PreWorldGen()
	{
		scheduledResidencePass = null;
		string forcedProfile = Environment.GetEnvironmentVariable(WorldgenTestProfileEnvironmentVariable);
		if (!Main.dedServ || !string.Equals(forcedProfile, "for-the-worthy",
			StringComparison.OrdinalIgnoreCase))
			return;
		Main.getGoodWorld = true;
		Main.ActiveWorldFileData?.SetSeed("for the worthy");
		string forcedEvil = Environment.GetEnvironmentVariable(WorldgenTestEvilEnvironmentVariable);
		string evilReport = "random";
		if (string.Equals(forcedEvil, "corruption", StringComparison.OrdinalIgnoreCase))
		{
			WorldGen.crimson = false;
			evilReport = "corruption";
		}
		else if (string.Equals(forcedEvil, "crimson", StringComparison.OrdinalIgnoreCase))
		{
			WorldGen.crimson = true;
			evilReport = "crimson";
		}
		else if (!string.IsNullOrWhiteSpace(forcedEvil))
			throw new InvalidOperationException(
				$"Unsupported DingDong worldgen test evil: {forcedEvil}.");
		Mod.Logger.Warn($"Worldgen test profile override active: for-the-worthy; numericSeed={Main.ActiveWorldFileData?.Seed}; worldEvil={evilReport}");
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		scheduledResidencePass = null;
		if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive) return;
		if (!ModContent.GetInstance<DingDongServerConfig>().GenerateAuthoredSettlements)
		{
			Mod.Logger.Info("Authored biome settlement generation disabled by server config.");
			return;
		}
		if (!NpcAuthoredPresetManifest.IsCandidateComplete ||
			!AuthoredCandidateDeploymentManifest.IsComplete)
		{
			Mod.Logger.Error($"Authored settlement generation withheld: technical candidate/deployment manifests are incomplete ({NpcAuthoredPresetManifest.CandidateCount}/{NpcTownManifest.AuthoredCount}, {AuthoredCandidateDeploymentManifest.Count}/{NpcTownManifest.AuthoredCount}). Procedural fallback is forbidden.");
			return;
		}
		if (!ContinentalWorldDimensions.SupportsAuthoredSettlements)
		{
			// Presets 28.10 (design step 10): vanilla sizes (and unreleased presets) are skipped.
			Mod.Logger.Warn($"DINGDONG_WORLDGEN_SETTLEMENTS|verdict=SKIPPED|reason=not-a-continental-preset|current={Main.maxTilesX}x{Main.maxTilesY}" +
				$"|available={string.Join(",", System.Linq.Enumerable.Select(Worldgen.ContinentalWorldDimensions.Available, p => p.Id))}" +
				". Authored 61-residence generation needs a Ding Dong world preset; generation skipped without procedural fallback.");
			return;
		}

		int receipt = FindPassIndex(tasks, ContinentalPassNames.Receipt, out int receiptCount);
		if (receiptCount > 1)
		{
			Mod.Logger.Error($"DINGDONG_WORLDGEN_INSERTION|verdict=WITHHELD|reason=duplicate-natural-receipt|count={receiptCount}|mutated=0");
			return;
		}
		var authored = new PassLegacy(ContinentalPassNames.AuthoredResidences,
			GenerateHomes, PassWeight);
		scheduledResidencePass = authored;
		if (receiptCount == 1)
			tasks.Insert(receipt + 1, authored);
		else
			tasks.Add(authored);
		totalWeight += PassWeight;
	}

	internal static void FinalizeResidencePass(Mod owner, List<GenPass> tasks)
	{
		if (scheduledResidencePass == null || !scheduledResidencePass.Enabled) return;
		bool moved = WorldgenPassBoundary.MoveOwnedLast(tasks, scheduledResidencePass,
			pass => pass.Name, ContinentalPassNames.AuthoredResidences);
		owner.Logger.Info($"DINGDONG_STAGE_BOUNDARY|consumer=authored-residences|after=all-provider-passes" +
			$"|moved={moved}|foreignOrderUnchanged=True|passCount={tasks.Count}");
	}

	private static int FindPassIndex(List<GenPass> tasks, string name, out int count)
	{
		int found = -1;
		count = 0;
		for (int index = 0; index < tasks.Count; index++)
			if (tasks[index].Name == name)
			{
				found = index;
				count++;
			}
		return count == 1 ? found : -1;
	}
}
