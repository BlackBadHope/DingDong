using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
[Autoload(false)]
public sealed class GeneratedSynergyArmorItem : ModItem
{
	[CloneByReference]
	private readonly SynergyArmorSetDefinition definition;
	private readonly SynergyArmorPiece piece;
	private readonly string internalName;
	internal SynergyArmorSetDefinition Definition => definition;
	internal SynergyArmorPiece Piece => piece;
	protected override bool CloneNewInstances => true;
	public override string Name => internalName;
	public override string Texture => $"DingDongBiomeHomes/Items/SynergyArmor/Generated/{internalName}";
	public override LocalizedText DisplayName => Language.GetOrRegister($"Mods.DingDongBiomeHomes.Items.{internalName}.DisplayName", () => definition.DisplayNameFor(piece));
	public override LocalizedText Tooltip => Language.GetOrRegister($"Mods.DingDongBiomeHomes.SynergyArmor.LiveTooltipV3.{internalName}", () => string.Empty);

	internal GeneratedSynergyArmorItem(SynergyArmorSetDefinition definition, SynergyArmorPiece piece)
	{
		this.definition = definition;
		this.piece = piece;
		internalName = definition.InternalName(piece);
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.defense = definition.DefenseFor(piece);
		Item.rare = definition.Rarity;
		Item.value = definition.Value;
		Item.maxStack = 1;
		Item.accessory = false;
		Item.vanity = false;
		Item.material = false;
	}

	public override void UpdateEquip(Player player)
	{
		float band = (int)definition.Band;
		switch (piece)
		{
			case SynergyArmorPiece.Head:
				player.GetDamage(DamageClass.Generic) += 0.02f + 0.01f * band;
				player.GetCritChance(DamageClass.Generic) += 1f + band;
				break;
			case SynergyArmorPiece.Body:
				player.endurance += 0.005f + 0.005f * band;
				player.lifeRegen += 1 + (int)band / 2;
				break;
			case SynergyArmorPiece.Legs:
				player.moveSpeed += 0.04f + 0.01f * band;
				break;
		}
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		// Resolve at draw time so Russian clients do not inherit the generated English
		// fallback captured while dynamic items were being registered.
		tooltips.Add(new TooltipLine(Mod, "SynergyArmorPieceEffect", PieceEffect()));
		tooltips.Add(new TooltipLine(Mod, "SynergyArmorAcquisition", SynergyArmorText.Format("Labels.Acquisition", "Acquisition: {0}", definition.LocalizedAcquisition())));
	}

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe();
		foreach (SynergyArmorIngredient ingredient in definition.Ingredients)
		{
			int count = ingredient.Count(piece);
			if (count > 0)
				recipe.AddIngredient(ingredient.ItemType, count);
		}

		int? relicType = RequiredRelicType(definition);
		if (relicType.HasValue)
			recipe.AddIngredient(relicType.Value);

		recipe.AddTile(definition.CraftingTile);
		recipe.Register();
	}

	private static int? RequiredRelicType(SynergyArmorSetDefinition armor)
	{
		bool hasSource = !string.IsNullOrWhiteSpace(armor.RelicSource);
		bool hasTier = armor.RelicTier.HasValue;
		if (!hasSource && !hasTier)
			return null;
		if (!hasSource || !hasTier || !MobSpecificRelicCatalog.TryGet(armor.RelicSource, out MobRelicSet relicSet))
			throw new InvalidOperationException($"Armor {armor.Key} has an unresolved relic source/tier: {armor.RelicSource ?? "<none>"}/{armor.RelicTier?.ToString() ?? "<none>"}.");
		int tierIndex = (int)armor.RelicTier.Value;
		if ((uint)tierIndex >= (uint)relicSet.Items.Count || relicSet.Items[tierIndex].Type <= ItemID.None)
			throw new InvalidOperationException($"Armor {armor.Key} resolved an invalid relic item at tier {armor.RelicTier.Value}.");
		return relicSet.Items[tierIndex].Type;
	}

	private string PieceEffect() => piece switch
	{
		SynergyArmorPiece.Head => SynergyArmorText.Format("Pieces.Head", "+{0}% damage and +{1}% critical strike chance", 2 + (int)definition.Band, 1 + (int)definition.Band),
		SynergyArmorPiece.Body => SynergyArmorText.Format("Pieces.Body", "+{0}% damage reduction and +{1} life regeneration", (0.5f + 0.5f * (int)definition.Band).ToString("0.#", CultureInfo.InvariantCulture), 1 + (int)definition.Band / 2),
		_ => SynergyArmorText.Format("Pieces.Legs", "+{0}% movement speed", 4 + (int)definition.Band)};
}
