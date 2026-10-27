using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

// A recipe describing the contents of a bag is not a reversible manufacturing
// recipe. Reversing it manufactures another roll of the loot table.
internal static class UncraftReversibility
{
    private static readonly Dictionary<int, bool> containers = new();

    internal static bool IsMaterialRecipe(Recipe recipe)
    {
        if (recipe?.createItem == null || recipe.createItem.IsAir || recipe.createItem.stack <= 0 ||
            recipe.DecraftDisabled || recipe.requiredItem.Count == 0) return false;
        foreach (Item ingredient in recipe.requiredItem)
            if (ingredient.IsAir || ingredient.stack <= 0 || ingredient.type == recipe.createItem.type || IsLootContainer(ingredient.type))
                return false;
        return true;
    }

    internal static bool IsLootContainer(int type)
    {
        if (type <= 0 || type >= ItemLoader.ItemCount) return false;
        if (containers.TryGetValue(type, out bool found)) return found;
        bool result = ItemID.Sets.IsFishingCrate[type] || ItemID.Sets.BossBag[type] || ItemID.Sets.OpenableBag[type] ||
            Main.ItemDropsDB.GetRulesForItemID(type).Count > 0;
        // Some third-party bags still grant loot in RightClick instead of item
        // loot rules. Stackable custom right-click items also cannot be used as
        // a reversible ingredient; this does not affect their opening or drops.
        if (!result && ContentSamples.ItemsByType.TryGetValue(type, out Item sample) && sample.maxStack > 1 && sample.ModItem != null)
        {
            MethodInfo rightClick = sample.ModItem.GetType().GetMethod(nameof(ModItem.RightClick), new[] { typeof(Player) });
            result = rightClick?.DeclaringType != typeof(ModItem);
        }
        containers[type] = result;
        return result;
    }

    internal static void Reset() => containers.Clear();
}
