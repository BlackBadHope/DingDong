using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using DingDongBiomeHomes.Fishing.Content;

namespace DingDongBiomeHomes.Fishing.Crates;

internal sealed record LockpickDefinition(string Id, string En, string Ru, Color Color, int Ingredient, int IngredientCount = 1, bool Iron = true);

internal static class CrateLockpickCatalog
{
    internal static readonly LockpickDefinition[] Definitions = {
        new("Iron", "Reinforced-crate lockpick", "Отмычка для укреплённых ящиков", new(180,194,206), ItemID.Wood, 2),
        new("Gold", "Gilded-crate lockpick", "Отмычка для позолоченных ящиков", new(255,211,78), ItemID.GoldOre),
        new("Corrupt", "Corruption lockpick", "Отмычка порчи", new(160,112,224), ItemID.EbonstoneBlock, 5),
        new("Crimson", "Crimson lockpick", "Отмычка багрянца", new(234,102,119), ItemID.CrimstoneBlock, 5),
        new("Dungeon", "Dungeon lockpick", "Отмычка данжа", new(128,159,197), ItemID.Bone, 2),
        new("Sky", "Sky lockpick", "Небесная отмычка", new(236,235,161), ItemID.Cloud, 5),
        new("Hallow", "Hallowed lockpick", "Освящённая отмычка", new(227,159,240), ItemID.PearlstoneBlock, 5),
        new("Jungle", "Jungle lockpick", "Отмычка джунглей", new(139,206,99), ItemID.RichMahogany,5),
        new("Frozen", "Frozen lockpick", "Ледяная отмычка", new(127,215,249), ItemID.IceBlock, 5),
        new("Oasis", "Oasis lockpick", "Отмычка оазиса", new(234,191,121), ItemID.SandBlock, 5),
        new("Lava", "Obsidian lockpick", "Обсидиановая отмычка", new(202,122,91), ItemID.Obsidian, 2),
        new("Ocean", "Ocean lockpick", "Морская отмычка", new(100,208,191), ItemID.Coral),
        new("Cave", "Cavern lockpick", "Пещерная отмычка", new(168,166,200), ItemID.StoneBlock, 5),
        new("Abyss", "Abyssal lockpick", "Отмычка бездны", new(117,149,171), ItemID.Obsidian, 2),
        new("Expedition", "Expedition lockpick", "Экспедиционная отмычка", new(118,222,199), ItemID.Coral),
        new("Astral", "Starlit lockpick", "Звёздная отмычка",new(168,165,244),ItemID.FallenStar),
        new("Blood", "Blood-moon lockpick", "Отмычка кровавой луны",new(221,108,124),ItemID.RottenChunk),
        new("Shadow", "Eclipse lockpick", "Отмычка затмения",new(164,139,188),ItemID.Obsidian),
        new("Mushroom", "Mushroom lockpick", "Грибная отмычка",new(113,161,241),ItemID.GlowingMushroom,2),
        new("Honey", "Hive lockpick", "Ульевая отмычка",new(242,187,79),ItemID.Hive,5),
        new("Temple", "Temple lockpick", "Храмовая отмычка",new(204,157,89),ItemID.ClayBlock,5),
        new("Marble", "Marble lockpick", "Мраморная отмычка",new(216,215,200),ItemID.Marble,5),
        new("Meteor", "Meteoric lockpick", "Метеоритная отмычка",new(207,148,108),ItemID.Meteorite),
        new("Festive", "Festive lockpick", "Праздничная отмычка",new(146,208,184),ItemID.SnowBlock,5),
        new("Harvest", "Harvest lockpick", "Тыквенная отмычка",new(239,153,72),ItemID.Pumpkin,2)
    };
    private static readonly Dictionary<string, int> picks = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, string> crates = new();
    internal static IReadOnlyDictionary<int, string> Crates => crates;
    internal static void Register(Mod mod)
    {
        foreach (var definition in Definitions) mod.AddContent(new CrateLockpick(definition));
    }
    internal static void Initialize()
    {
        picks.Clear(); crates.Clear();
        foreach (var definition in Definitions) picks.Add(definition.Id, ModContent.Find<ModItem>("DingDongBiomeHomes/" + definition.Id + "CrateLockpick").Type);
        foreach (var pair in ContentSamples.ItemsByType)
        {
            int type = pair.Key; Item item = pair.Value;
            // Wooden crates are the free early-game tier: they open without a pick.
            if (type is ItemID.WoodenCrate or ItemID.WoodenCrateHard) continue;
            if (ItemID.Sets.IsFishingCrate[type]) crates[type] = Theme(item);
            else if (item.ModItem is GeneratedFishingItem generated && generated.Definition.Kind == FishingContentKind.Treasure)
                crates[type] = generated.Definition.Habitat switch {
                    FishingHabitat.Abyss => "Abyss", FishingHabitat.CaveLake or FishingHabitat.UndergroundRiver => "Cave", _ => "Ocean"
                };
        }
    }
    internal static int PickType(string theme) => picks.TryGetValue(theme, out int type) ? type : 0;
    internal static int ApplyRecipeCosts()
    {
        int count=0;
        foreach(var recipe in Main.recipe.Take(Recipe.numRecipes))
        {
            var costs=new Dictionary<int,int>();
            foreach(var item in recipe.requiredItem)
                if(TryGet(item.type,out int key))costs[key]=costs.GetValueOrDefault(key)+item.stack;
            foreach(var cost in costs)recipe.AddIngredient(cost.Key,cost.Value);
            if(costs.Count>0)count++;
            if(ItemID.Sets.IsFishingCrateHardmode[recipe.createItem.type] && recipe.requiredItem.Any(i=>ItemID.Sets.IsFishingCrate[i.type]&&!ItemID.Sets.IsFishingCrateHardmode[i.type]) && !recipe.Conditions.Contains(Condition.Hardmode))
                recipe.AddCondition(Condition.Hardmode);
        }
        return count;
    }
    internal static string BonusMaterial(int crate)
    {
        string theme=crates.GetValueOrDefault(crate,"Iron");
        return theme switch {"Ocean"=>"ocean_glass","Abyss"=>"abyssal_steel","Cave"=>"cave_salt",_=>"salvage_fragment"};
    }
    internal static bool TryGet(int crate, out int pick)
    {
        pick = 0; return crates.TryGetValue(crate, out string theme) && picks.TryGetValue(theme, out pick);
    }
    private static string Theme(Item item)
    {
        int type = item.type;
        if (type is ItemID.IronCrate or ItemID.IronCrateHard) return "Iron";
        if (type is ItemID.GoldenCrate or ItemID.GoldenCrateHard) return "Gold";
        string name = (item.ModItem?.Name ?? ItemID.Search.GetName(type)).ToLowerInvariant();
        if(name.Contains("bloodmoon")||name.Contains("scarlet"))return "Blood";
        if(name.Contains("eclipse")||name.Contains("sinister"))return "Shadow";
        if(name.Contains("astral")||name.Contains("monolith")||name.Contains("planetarium")||name.Contains("otherworld"))return "Astral";
        if(name.Contains("glowmushroom"))return "Mushroom";
        if(name.Contains("hive"))return "Honey";
        if(name.Contains("lihzahrd"))return "Temple";
        if(name.Contains("marble"))return "Marble";
        if(name.Contains("martian"))return "Meteor";
        if(name.Contains("present"))return "Festive";
        if(name.Contains("pumpkin"))return "Harvest";
        if(name.Contains("cavern"))return "Cave";
        if(name.Contains("pyramid"))return "Oasis";
        if(name.Contains("eutrophic")||name.Contains("prism")||name.Contains("aquaticdepth"))return "Ocean";
        if (name.Contains("corrupt")) return "Corrupt";
        if (name.Contains("crimson")) return "Crimson";
        if (name.Contains("dungeon")) return "Dungeon";
        if (name.Contains("floating") || name.Contains("sky")) return "Sky";
        if (name.Contains("hallow")) return "Hallow";
        if (name.Contains("jungle")) return "Jungle";
        if (name.Contains("frozen") || name.Contains("ice") || name.Contains("boreal")) return "Frozen";
        if (name.Contains("oasis") || name.Contains("desert")) return "Oasis";
        if (name.Contains("lava") || name.Contains("brim") || name.Contains("obsidian")||name.Contains("slag")||name.Contains("hydrothermal")) return "Lava";
        if (name.Contains("abyss") || name.Contains("sulph")) return "Abyss";
        if (name.Contains("ocean") || name.Contains("sea") || name.Contains("sunken")) return "Ocean";
        return "Iron";
    }
    internal static void Reset() { picks.Clear(); crates.Clear(); }
}
