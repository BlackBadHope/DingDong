using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
public readonly record struct AccessoryEnchantingAuditResult(bool Passed, int LevelsPassed, int FusionsPassed, int AtomicRejectionsPassed, int PersistencePassed, int RuntimeEffectsPassed, int StationFlowsPassed, int ArtAssetsExpected, IReadOnlyList<string> Failures)
{
	public string ToMarker() => string.Join('|', new[] { "DINGDONG_ACCESSORY_ENCHANTING_E2E", $"verdict={(Passed ? "PASS" : "FAIL")}", $"levels={LevelsPassed}/5", $"fusions={FusionsPassed}/5", $"atomicRejections={AtomicRejectionsPassed}/3", $"persistence={PersistencePassed}/3", $"runtimeEffects={RuntimeEffectsPassed}/4", $"stationFlows={StationFlowsPassed}/4", $"artAssets={ArtAssetsExpected}/16", $"failures={OneLine(string.Join(',', Failures))}", "userSavesTouched=False" });
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
