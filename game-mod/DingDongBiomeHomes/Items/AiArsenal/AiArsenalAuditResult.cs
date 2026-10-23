using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public readonly record struct AiArsenalAuditResult(bool Passed, int CategoriesPassed, int ItemsPassed, int TiersPassed, int TexturesPassed, int LocalizationFieldsPassed, int RecipesPassed, int RecipesExpected, int DamageClassesPassed, int ClassificationsPassed, int DamageCasesPassed, int DamageCasesExpected, IReadOnlyList<string> Failures)
{
	public string ToMarker() => string.Join('|', new[] { "DINGDONG_AI_ARSENAL_E2E", $"verdict={(Passed ? "PASS" : "FAIL")}", $"categories={CategoriesPassed}/4", $"items={ItemsPassed}/20", $"tiers={TiersPassed}/20", $"textures={TexturesPassed}/20", $"localization={LocalizationFieldsPassed}/40", $"recipes={RecipesPassed}/{RecipesExpected}", $"damageClasses={DamageClassesPassed}/20", $"classifications={ClassificationsPassed}/20", $"damageCases={DamageCasesPassed}/{DamageCasesExpected}", $"failures={OneLine(string.Join(',', Failures))}", "userSavesTouched=False" });
	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
