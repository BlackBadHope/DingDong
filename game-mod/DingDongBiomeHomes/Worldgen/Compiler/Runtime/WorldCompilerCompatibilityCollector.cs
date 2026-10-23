using System;
using System.Collections.Generic;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerCompatibilityCollector
{
	internal static bool TryCollect(List<GenPass> tasks, out List<GenPass> selected,
		out string receipt)
	{
		selected = [];
		foreach (GenPass task in tasks)
		{
			if (string.IsNullOrWhiteSpace(task.Name))
				return Withhold(selected, "unnamed-pass", out receipt);
			if (task.Name.StartsWith("DingDong Compiler:", StringComparison.Ordinal))
				return Withhold(selected, $"recursive-pass:{task.Name}", out receipt);
			if (!WorldCompilerVanillaPassRegistry.Contains(task.Name)) selected.Add(task);
		}
		if (selected.Count < 1)
			return Withhold(selected, "external-pass-set-empty", out receipt);
		var selectedNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (GenPass pass in selected) selectedNames.Add(pass.Name);
		foreach (Exact66WorldgenProviderContract contract in
			Exact66WorldgenProviderRegistry.Entries)
		{
			if (string.IsNullOrWhiteSpace(contract.PassName)) continue;
			if (!selectedNames.Contains(contract.PassName))
				return Withhold(selected, $"anchor-missing:{contract.PassName}", out receipt);
		}
		ulong fingerprint = 1469598103934665603UL;
		foreach (GenPass pass in selected)
			foreach (char value in pass.Name)
			{
				fingerprint ^= value;
				fingerprint *= 1099511628211UL;
			}
		receipt = $"passes={selected.Count};vanillaRegistry=" +
			$"{WorldCompilerVanillaPassRegistry.Count};fingerprint={fingerprint:X16}";
		return true;
	}

	private static bool Withhold(List<GenPass> selected, string reason,
		out string receipt)
	{
		selected.Clear();
		receipt = reason;
		return false;
	}
}
