using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

/// <summary>
/// Keeps recipe prototypes compatible with the concrete crafted stack. Vanilla
/// compares the cursor item with Recipe.createItem before every held-click
/// repeat; both must carry the same per-entity acquisition metadata.
/// </summary>
public sealed class DamageAffinityRecipeSystem : ModSystem
{
	public override void PostAddRecipes()
	{
		for (int index = 0; index < Recipe.numRecipes; index++)
		{
			Recipe recipe = Main.recipe[index];
			if (recipe?.createItem is not null)
				DamageAffinityGlobalItem.MarkRecipePrototype(recipe.createItem);
		}
	}
}
