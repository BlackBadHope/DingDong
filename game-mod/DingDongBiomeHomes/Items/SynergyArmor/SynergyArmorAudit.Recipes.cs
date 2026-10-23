using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;

namespace DingDongBiomeHomes.Items.SynergyArmor;

internal static partial class SynergyArmorAudit
{
	private static bool RecipeHasExactRelicContract(GeneratedSynergyArmorItem armor, out string failure)
	{
		Recipe[] recipes = Main.recipe.Take(Recipe.numRecipes).Where(recipe => recipe?.createItem.type == armor.Type).ToArray();
		if (recipes.Length != 1)
		{
			failure = $"recipe:{armor.Name}:{recipes.Length}/1";
			return false;
		}

		List<Item> relicIngredients = recipes[0].requiredItem.Where(item => item.ModItem is GeneratedMobRelicItem).ToList();
		bool hasSource = !string.IsNullOrWhiteSpace(armor.Definition.RelicSource);
		bool hasTier = armor.Definition.RelicTier.HasValue;
		if (!hasSource && !hasTier)
		{
			failure = $"recipe-relic:{armor.Name}:{relicIngredients.Count}/0";
			return relicIngredients.Count == 0;
		}
		if (!hasSource || !hasTier || !MobSpecificRelicCatalog.TryGet(armor.Definition.RelicSource, out MobRelicSet set))
		{
			failure = $"recipe-relic-unresolved:{armor.Name}";
			return false;
		}

		int tierIndex = (int)armor.Definition.RelicTier.Value;
		if ((uint)tierIndex >= (uint)set.Items.Count)
		{
			failure = $"recipe-relic-tier:{armor.Name}:{tierIndex}";
			return false;
		}
		int expectedType = set.Items[tierIndex].Type;
		int exact = relicIngredients.Count(item => item.type == expectedType && item.stack == 1);
		failure = $"recipe-relic:{armor.Name}:{exact}/1,total={relicIngredients.Count}";
		return relicIngredients.Count == 1 && exact == 1;
	}
}
