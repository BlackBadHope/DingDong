using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
public readonly record struct SynergyArmorAuditResult(bool Passed, int SetsPassed, int ItemsPassed, int WearableSlotsPassed, int ProgressionBandsPassed, int RecipesPassed, int SetHooksPassed, int RuntimeMechanicsPassed, int RelicLinksPassed, IReadOnlyList<string> Failures)
{
	public string ToMarker() => string.Join('|', new[] { "DINGDONG_SYNERGY_ARMOR_E2E", $"verdict={(Passed ? "PASS" : "FAIL")}", $"sets={SetsPassed}/15", $"items={ItemsPassed}/45", $"wearableSlots={WearableSlotsPassed}/45", $"bands={ProgressionBandsPassed}/5", $"recipes={RecipesPassed}/45", $"setHooks={SetHooksPassed}/15", $"runtimeMechanics={RuntimeMechanicsPassed}/15", $"relicLinks={RelicLinksPassed}/15", $"failures={OneLine(string.Join(',', Failures))}", "userSavesTouched=False" });
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
