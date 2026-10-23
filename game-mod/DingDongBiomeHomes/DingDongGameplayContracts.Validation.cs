using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class DingDongGameplayContracts
{
	internal static GameplayContractResult EvaluateStabilityGate(GameplayContractResult gameplay)
	{
		List<string> failures = new();
		string version = DingDongBiomeHomes.Instance?.Version.ToString() ?? "missing";
		if (!gameplay.Pass)
			failures.Add("gameplay=FAIL");
		if (!DeterministicConstraintSolver.TrySelfTest(out string solverFailure))
			failures.Add($"solver={solverFailure}");
		if (!DeterministicConstraintRepairSolver.TrySelfTest(out string repairFailure))
			failures.Add($"repair={repairFailure}");
		if (!DeterministicConstraintBeamSolver.TrySelfTest(out string beamFailure))
			failures.Add($"beam={beamFailure}");
		if (!DeterministicArcConsistencyPruner.TrySelfTest(out string arcFailure))
			failures.Add($"arc={arcFailure}");
		if (!NpcQuestNetwork.ContractPass(out string questNetworkFailure))
			failures.Add($"questNetwork={questNetworkFailure}");
		if (!NpcResidenceWorldSystem.NetworkContractPass(out string escortFailure))
			failures.Add($"escort={escortFailure}");
		string verdict = failures.Count == 0 ? "PASS" : "FAIL";
		string details = failures.Count == 0 ? "none" : string.Join(",", failures);
		return new GameplayContractResult(failures.Count == 0, $"DINGDONG_STABILITY_GATE|verdict={verdict}|version={version}|authored={NpcTownManifest.AuthoredCount}/61|visualApproved={NpcAuthoredPresetManifest.Count}/61|multiplayer=NOT_TESTED|wireContracts={(questNetworkFailure.Length == 0 && escortFailure.Length == 0 ? "PASS" : "FAIL")}|gameplayContracts={(gameplay.Pass ? "PASS" : "FAIL")}|failures={details}");
	}
}
