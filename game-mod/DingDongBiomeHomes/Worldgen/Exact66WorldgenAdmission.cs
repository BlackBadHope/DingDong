using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class Exact66WorldgenAdmission
{
	internal static bool TryAdmit(Mod owner, List<GenPass> tasks,
		out int lastNaturalIndex, out string receipt)
	{
		lastNaturalIndex = -1;
		var observed = new List<string>();
		foreach (Exact66WorldgenProviderContract contract in Exact66WorldgenProviderRegistry.Entries)
		{
			if (!ModLoader.TryGetMod(contract.ModName, out Mod provider))
				return Withhold(owner, "unknown-provider", contract.ModName, out receipt);
			if (!string.Equals(provider.Version.ToString(), contract.Version, StringComparison.Ordinal))
				return Withhold(owner, "unknown-provider-version",
					$"{contract.ModName}:{provider.Version}!={contract.Version}", out receipt);
			Guid actual = provider.Code?.ManifestModule.ModuleVersionId ?? Guid.Empty;
			if (!Guid.TryParse(contract.ModuleMvid, out Guid expected) || actual != expected)
				return Withhold(owner, "fingerprint-mismatch",
					$"{contract.ModName}:{actual}!={contract.ModuleMvid}", out receipt);
			if (contract.PassClass == Exact66WorldgenPassClass.OrderingOnly)
			{
				observed.Add($"{contract.ModName}:ordering-only");
				continue;
			}
			int index = FindUnique(tasks, contract.PassName, out int count);
			if (count != 1)
				return Withhold(owner, "pass-topology-mismatch",
					$"{contract.ModName}:{contract.PassName}:count={count}", out receipt);
			observed.Add($"{contract.ModName}:{contract.PassClass}@{index}");
			if (contract.PassClass == Exact66WorldgenPassClass.Natural && index > lastNaturalIndex)
				lastNaturalIndex = index;
		}
		if (lastNaturalIndex < 0)
			return Withhold(owner, "natural-anchor-missing", "registry", out receipt);
		receipt = string.Join(',', observed);
		owner.Logger.Info($"DINGDONG_EXACT66_WORLDGEN|verdict=ARMED|providers={Exact66WorldgenProviderRegistry.Entries.Length}|lastNatural={lastNaturalIndex}|classification={receipt}|rerunPasses=0");
		return true;
	}

	private static int FindUnique(List<GenPass> tasks, string name, out int count)
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

	private static bool Withhold(Mod owner, string reason, string detail, out string receipt)
	{
		receipt = string.Empty;
		owner.Logger.Error($"DINGDONG_EXACT66_WORLDGEN|verdict=WITHHELD|reason={reason}|detail={detail}|mutated=0|rerunPasses=0");
		return false;
	}
}
