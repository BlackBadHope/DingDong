using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal static partial class AiArsenalAudit
{
	internal static AiArsenalAuditResult Run(Mod mod, bool requireRecipes, bool exerciseDamage)
	{
		List<AiArsenalItem> items = ModContent.GetContent<AiArsenalItem>().Where(item => ReferenceEquals(item.Mod, mod)).OrderBy(item => item.Category).ThenBy(item => item.Tier).ToList();
		List<string> failures = new();
		int categoriesPassed = 0;
		foreach (AiArsenalCategory category in Enum.GetValues<AiArsenalCategory>())
		{
			AiArsenalItem[] categoryItems = items.Where(item => item.Category == category).ToArray();
			bool categoryPassed = categoryItems.Length == 5 && categoryItems.Select(item => item.Tier).Distinct().Count() == 5;
			if (categoryPassed)
				categoriesPassed++;
			else
				failures.Add($"category:{category}:{categoryItems.Length}");
		}

		int itemsPassed = 0;
		int tiersPassed = 0;
		int texturesPassed = 0;
		int localizationPassed = 0;
		int recipesPassed = 0;
		int recipesExpected = requireRecipes ? 24 : 0;
		int damageClassesPassed = 0;
		int classificationsPassed = 0;
		int damageCasesPassed = 0;
		int damageCasesExpected = exerciseDamage ? 20 : 0;
		DamageRuleEngine engine = new();
		DamageHitLedger ledger = new();
		NPC target = new();
		target.SetDefaults(NPCID.BlueSlime);
		target.whoAmI = 0;
		DamageTargetClassification targetProfile = DamageRuntimeClassifier.Classify(target);
		int caseIndex = 0;
		foreach (AiArsenalItem modItem in items)
		{
			Item item = RunPhase(mod, failures, ref itemsPassed, ref tiersPassed, ref texturesPassed, ref localizationPassed, ref caseIndex, modItem);
			if (requireRecipes)
			{
				int expected = modItem.Tier == AiArsenalTier.Gilded ? 2 : 1;
				int actual = Main.recipe.Take(Recipe.numRecipes).Count(recipe => recipe?.createItem.type == modItem.Type);
				recipesPassed += Math.Min(actual, expected);
				if (actual != expected)
					failures.Add($"recipes:{modItem.Name}:{actual}/{expected}");
			}

			RunValidDamageClass(exerciseDamage, failures, ref damageClassesPassed, ref classificationsPassed, ref damageCasesPassed, engine, ledger, target, targetProfile, caseIndex, modItem, item);
		}

		bool passed = items.Count == 20 && categoriesPassed == 4 && itemsPassed == 20 && tiersPassed == 20 && texturesPassed == 20 && localizationPassed == 40 && (!requireRecipes || recipesPassed == recipesExpected) && damageClassesPassed == 20 && classificationsPassed == 20 && (!exerciseDamage || damageCasesPassed == damageCasesExpected) && failures.Count == 0;
		AiArsenalAuditResult result = new(passed, categoriesPassed, itemsPassed, tiersPassed, texturesPassed, localizationPassed, recipesPassed, recipesExpected, damageClassesPassed, classificationsPassed, damageCasesPassed, damageCasesExpected, failures);
		if (passed)
			mod.Logger.Info(result.ToMarker());
		else
			mod.Logger.Error(result.ToMarker());
		return result;
	}
}
