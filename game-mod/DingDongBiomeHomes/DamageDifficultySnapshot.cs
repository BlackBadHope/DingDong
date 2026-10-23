using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed record DamageDifficultySnapshot(string VanillaMode, string SeedContext, bool DingDongEnabled, IReadOnlyList<DamageDifficultyEntry> ExternalModes)
{
	public int AdapterFailures
	{
		get
		{
			int failures = 0;
			foreach (DamageDifficultyEntry entry in ExternalModes)
			{
				if (!entry.AdapterHealthy)
					failures++;
			}

			return failures;
		}
	}

	public string ToMarker()
	{
		List<string> adapterStates = new();
		foreach (DamageDifficultyEntry entry in ExternalModes)
			adapterStates.Add($"{OneLine(entry.Source)}:{entry.AdapterHealthy}:{OneLine(entry.Mode)}:{OneLine(entry.Detail)}");
		return string.Join('|', new[] { "DINGDONG_DAMAGE_DIFFICULTY", $"vanilla={OneLine(VanillaMode)}", $"seed={OneLine(SeedContext)}", $"worldReady={DamageDifficultyContextSystem.IsWorldReady}", $"masterMode={Main.masterMode}", $"forTheWorthy={Main.getGoodWorld}", $"legendary={Main.masterMode && Main.getGoodWorld}", $"dingDongEnabled={DingDongEnabled}", $"externalAdapters={ExternalModes.Count}", $"adapterFailures={AdapterFailures}", $"adapterStates={string.Join(',', adapterStates)}", $"verdict={(AdapterFailures == 0 ? "PASS" : "FAIL")}" });
	}

	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
