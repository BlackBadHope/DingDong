using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Crates;

[Autoload(false)]
public sealed class CrateLockpick : ModItem
{
    [CloneByReference] private readonly LockpickTier tier;
    protected override bool CloneNewInstances => true;
    public override string Name => tier.Id + "CrateLockpick";
    public override string Texture => "DingDongBiomeHomes/Assets/Items/CrateLockpick";
    public override LocalizedText DisplayName => Language.GetOrRegister("Mods.DingDongBiomeHomes.CrateLocks.Items." + Name + ".DisplayName", () => tier.En);
    public override LocalizedText Tooltip => Language.GetOrRegister("Mods.DingDongBiomeHomes.CrateLocks.Items." + Name + ".Tooltip", () => $"Opens fished crates with a {tier.ChancePercent}% chance. Never consumed; a failed attempt breaks the crate.");
    internal CrateLockpick(LockpickTier tier) => this.tier = tier;
    internal LockpickTier Tier => tier;
    public override void SetStaticDefaults() { Item.ResearchUnlockCount = 1; }
    public override void SetDefaults()
    {
        Item.width = 18; Item.height = 28; Item.maxStack = 1;
        Item.rare = tier.Level switch { 1 => ItemRarityID.Blue, 2 => ItemRarityID.LightRed, 3 => ItemRarityID.Lime, _ => ItemRarityID.Red };
        Item.value = Item.sellPrice(gold: tier.Level);
    }
    public override Color? GetAlpha(Color lightColor) => tier.Color;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (tier.Relics.Length > 1) recipe.AddRecipeGroup(tier.RelicGroup, 1);
        else recipe.AddIngredient(tier.Relics[0]);
        recipe.AddTile(TileID.Anvils).Register();
    }
}

public sealed class CrateLockpickContent : ModSystem
{
    public override void Load() => CrateLockpickCatalog.Register(Mod);
    public override void AddRecipeGroups()
    {
        foreach (var tier in CrateLockpickCatalog.Tiers.Where(t => t.Relics.Length > 1))
        {
            int first = tier.Relics[0];
            RecipeGroup.RegisterGroup(tier.RelicGroup, new RecipeGroup(
                () => Language.GetTextValue("LegacyMisc.37") + " " + Lang.GetItemNameValue(first), tier.Relics));
        }
    }
    public override void PostAddRecipes() => CrateLockpickCatalog.Initialize();
    public override void Unload() => CrateLockpickCatalog.Reset();
}
