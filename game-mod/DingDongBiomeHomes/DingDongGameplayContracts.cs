using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class DingDongGameplayContracts
{
	internal static GameplayContractResult Evaluate()
	{
		List<string> failures = new();
		if (NpcTownManifest.AuthoredCount != 61)
			failures.Add($"manifest={NpcTownManifest.AuthoredCount}/61");
		if (NpcResidentRegistry.ResolvedCount != NpcTownManifest.AuthoredCount)
			failures.Add($"resolved={NpcResidentRegistry.ResolvedCount}/{NpcTownManifest.AuthoredCount}");
		int invalidQuests = NpcTownManifest.All.Count(identity =>
		{
			ResidentQuestContract contract = ResidentQuestContract.For(identity);
			return contract.SupplyItemType <= 0 || contract.SupplyCount <= 0 || contract.DefenseKills <= 0;
		});
		if (invalidQuests != 0)
			failures.Add($"invalidQuestContracts={invalidQuests}");
		bool questRotation = ResidentQuestCatalog.ContractPass(out string questRotationFailure);
		if (!questRotation)
			failures.Add($"questRotation={questRotationFailure}");
		int unsafePartialWorldQuests = NpcTownManifest.All.Count(identity =>
			NpcQuestSystem.UsesResidenceMap(ResidentQuestCatalog.SelectAvailable(
				"partial-world-contract", identity, 0, allowResidenceObjectives: false).Kind));
		if (unsafePartialWorldQuests != 0)
			failures.Add($"partialWorldQuestFallbacks={unsafePartialWorldQuests}");
		bool localization = DingDongLocalizationContract.Pass(out string localizationFailure);
		if (!localization)
			failures.Add($"localization={localizationFailure}");
		bool questNetwork = NpcQuestNetwork.ContractPass(out string questNetworkFailure);
		if (!questNetwork)
			failures.Add($"questNetwork={questNetworkFailure}");
		bool uiPagination = DingDongPagination.ContractPass(out string uiPaginationFailure);
		if (!uiPagination)
			failures.Add($"uiPagination={uiPaginationFailure}");
		bool escortNetwork = NpcResidenceWorldSystem.NetworkContractPass(out string escortNetworkFailure);
		if (!escortNetwork)
			failures.Add($"escortNetwork={escortNetworkFailure}");
		bool settlement = NpcResidenceWorldSystem.SettlementContractPass();
		if (!settlement)
			failures.Add("settlement=invalid");
		bool trust = ResidentTrustPolicy.ContractPass();
		if (!trust)
			failures.Add("trust=invalid");
		int[] rewardCaches =
		{
			QuestRewardBalance.CacheItemType(0),
			QuestRewardBalance.CacheItemType(1),
			QuestRewardBalance.CacheItemType(2),
			QuestRewardBalance.CacheItemType(3)
		};
		bool rewardCacheContract = rewardCaches.All(type => type > 0) && rewardCaches.Distinct().Count() == 4;
		if (!rewardCacheContract)
			failures.Add("rewardCaches=invalid");
		bool rewardVariety = QuestThematicReward.ContractPass();
		if (!rewardVariety)
			failures.Add($"rewardVariety=npc<{QuestThematicReward.MinimumNpcVariety}|cache<{QuestThematicReward.MinimumCacheVariety}");
		if (!FiveTileJumpPlayer.ContractPass(out float apexPixels))
			failures.Add($"jumpApex={apexPixels:0.###}px");
		bool nightPressure = NightPressureGlobalNPC.ContractPass();
		if (!nightPressure)
			failures.Add("nightPressure=invalid");
		byte[] gameplayPacketIds = Enum.GetValues<DingDongPacketKind>().Select(value => (byte)value).ToArray();
		if (gameplayPacketIds.Distinct().Count() != gameplayPacketIds.Length || gameplayPacketIds.Any(DamageAuthoritySystem.IsPacketKind))
		{
			failures.Add("packetNamespace=collision");
		}

		bool teleporterPolicy = CreatureTeleporterSystem.IsEligibleContract(true, false, false, -1, 0, 32, 48, 6, 6) && !CreatureTeleporterSystem.IsEligibleContract(true, true, false, -1, 0, 32, 48, 6, 6) && !CreatureTeleporterSystem.IsEligibleContract(true, false, true, -1, 0, 32, 48, 6, 6) && !CreatureTeleporterSystem.IsEligibleContract(true, false, false, 1, 0, 32, 48, 6, 6) && !CreatureTeleporterSystem.IsEligibleContract(true, false, false, -1, Terraria.ID.NPCAIStyleID.Worm, 32, 48, 6, 6) && !CreatureTeleporterSystem.IsEligibleContract(true, false, false, -1, 0, 112, 48, 6, 6);
		if (!teleporterPolicy)
			failures.Add("teleporterExclusions=fail");
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		string details = failures.Count == 0 ? "none" : string.Join(",", failures);
		return new GameplayContractResult(failures.Count == 0, $"DINGDONG_GAMEPLAY_CONTRACT|verdict={verdict}|residents={NpcResidentRegistry.ResolvedCount}/{NpcTownManifest.AuthoredCount}|questSystem=5.0|questPool=5x{NpcTownManifest.AuthoredCount}|parallel=61|partialWorldFallbacks={(unsafePartialWorldQuests == 0 ? "61/61_PASS" : $"FAIL_{unsafePartialWorldQuests}")}|rewards=authored-items+coins|rewardVariety={(rewardVariety ? $"NPC{QuestThematicReward.MinimumNpcVariety}+_CACHE{QuestThematicReward.MinimumCacheVariety}+_PASS" : "FAIL")}|globalTargets=content-indexed|rotation={(questRotation ? "PERSONAL_STORIES_PASS" : "FAIL")}|questProtocol={(questNetwork ? NpcQuestNetwork.QuestProtocolVersion : 0)}|uiPagination={(uiPagination ? "PASS" : "FAIL")}|escortProtocol={(escortNetwork ? "PASS" : "FAIL")}|settlement={(settlement ? "TWO_LEG_1000_PASS" : "FAIL")}|trust={(trust ? "WORLD_SHARED_STABLE_PASS" : "FAIL")}|catalogFingerprint={QuestContentRegistry.CatalogFingerprint:X16}|localization={(localization ? "PASS" : "FAIL")}|jumpApex={apexPixels:0.###}px|jumpTarget=half-vanilla|nightPressure={(nightPressure ? "PASS" : "FAIL")}|teleporterPolicy={(teleporterPolicy ? "PASS" : "FAIL")}|failures={details}");
	}
}
