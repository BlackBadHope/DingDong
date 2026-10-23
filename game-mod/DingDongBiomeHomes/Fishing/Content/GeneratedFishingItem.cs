using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing.Content;

[Autoload(false)]
public sealed class GeneratedFishingItem : ModItem
{
	[CloneByReference]
	private readonly FishingContentDefinition definition;
	protected override bool CloneNewInstances => true;
	public override string Name => definition.InternalName;
	public override string Texture => "Terraria/Images/Item_" + definition.TextureItem;
	public override LocalizedText DisplayName => Language.GetOrRegister("Mods.DingDongBiomeHomes.Fishing.Items." + Name + ".DisplayName", () => definition.NameEn);
	public override LocalizedText Tooltip => Language.GetOrRegister("Mods.DingDongBiomeHomes.Fishing.Items." + Name + ".Tooltip", () => FishingContentText.Tooltip(definition, false));
	public FishingContentDefinition Definition => definition;

	internal GeneratedFishingItem(FishingContentDefinition definition) => this.definition = definition;

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = definition.Kind is FishingContentKind.Rig or FishingContentKind.Instrument ? 1 : 5;
		if (definition.Kind == FishingContentKind.Food) ItemID.Sets.IsFood[Type] = true;
		if (definition.Kind == FishingContentKind.Treasure) ItemID.Sets.OpenableBag[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 24;
		Item.maxStack = definition.Kind is FishingContentKind.Rig or FishingContentKind.Instrument ? 1 : Item.CommonMaxStack;
		Item.rare = definition.Rarity;
		Item.value = definition.Value;
		if (definition.Kind == FishingContentKind.Food)
		{
			Item.DefaultToFood(24, 24, BuffID.WellFed2, 12 * 60 * 60);
			Item.value = definition.Value;
		}
		else if (definition.Kind == FishingContentKind.Potion)
		{
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useTime = Item.useAnimation = 17;
			Item.UseSound = SoundID.Item3;
			Item.consumable = true;
			Item.buffType = definition.BuffType;
			Item.buffTime = definition.BuffMinutes * 60 * 60;
		}
		else if (definition.Kind == FishingContentKind.Instrument)
			Item.accessory = true;
	}

	public override bool CanRightClick() => definition.Kind is FishingContentKind.Rig or FishingContentKind.Treasure;
	public override bool ConsumeItem(Player player) => definition.Kind != FishingContentKind.Rig;
	public override void RightClick(Player player)
	{
		if (definition.Kind == FishingContentKind.Rig) player.GetModPlayer<FishingPlayer>().SelectRig(definition.RigId);
		// Cache contents are exclusively native item-loot rules. No second manual spawn path.
	}

	public override void OnConsumeItem(Player player)
	{
		if (definition.Kind == FishingContentKind.Food && definition.BuffType > 0)
			player.AddBuff(definition.BuffType, definition.BuffMinutes * 60 * 60);
	}

	public override void UpdateInventory(Player player)
	{
		if (definition.Id == "depth_compass") { player.accDepthMeter = 1; player.GetModPlayer<FishingPlayer>().EnableDepthSurvey(); }
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		if (definition.Id == "depth_compass") { player.accDepthMeter = 1; player.GetModPlayer<FishingPlayer>().EnableDepthSurvey(); }
		else if (definition.Id == "sounding_line")
		{
			player.fishingSkill += 10;
			player.accFishingLine = true;
		}
		else if (definition.Id == "diving_lantern")
		{
			player.accDivingHelm = true;
			if (!hideVisual) Lighting.AddLight(player.Center, new Vector3(0.16f, 0.30f, 0.38f));
		}
	}

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		if (definition.Kind != FishingContentKind.Treasure) return;
		itemLoot.Add(ItemDropRule.Common(FishingContentCatalog.GetItemType("salvage_fragment"), 1, 3, 7));
		itemLoot.Add(ItemDropRule.Common(FishingContentCatalog.GetItemType(definition.LootMaterial), 1, 2, 5));
		itemLoot.Add(ItemDropRule.Common(ItemID.SilverCoin, 1, 15, 40));
		itemLoot.Add(ItemDropRule.Common(ItemID.HealingPotion, 2, 2, 4));
		itemLoot.Add(ItemDropRule.Common(ItemID.ManaPotion, 2, 2, 4));
	}

	public override void AddRecipes()
	{
		foreach (FishingRecipeSpec definition in FishingContentCatalog.Recipes.Where(recipe => recipe.ResultId == this.definition.Id))
		{
			Recipe recipe = CreateRecipe(definition.Stack);
			foreach (FishingIngredient ingredient in definition.Ingredients)
			{
				if (ingredient.ItemId.Length > 0) recipe.AddIngredient(FishingContentCatalog.GetItemType(ingredient.ItemId), ingredient.Stack);
				else if (ingredient.Group.Length > 0) recipe.AddRecipeGroup(FishingContentCatalog.GroupName(ingredient.Group), ingredient.Stack);
				else recipe.AddIngredient(ingredient.VanillaItem, ingredient.Stack);
			}
			recipe.AddTile(definition.Tile);
			recipe.Register();
		}
	}
}
