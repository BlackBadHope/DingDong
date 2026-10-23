using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;
[Autoload(false)]
public sealed class GeneratedMobRelicItem : ModItem
{
	private readonly MobRelicSourceDefinition source;
	private readonly MobRelicTier tier;
	private readonly string internalName;
	private readonly string displayName;
	private readonly MobRelicEffectProfile profile;
	private readonly MobRelicVisualIdentity visualIdentity;
	internal MobRelicSourceDefinition Source => source;
	internal MobRelicTier Tier => tier;
	internal MobRelicEffectProfile Profile => profile;
	internal MobRelicVisualIdentity VisualIdentity => visualIdentity;
	internal string VisualSignature => visualIdentity.Signature;
	protected override bool CloneNewInstances => true;
	public override string Name => internalName;
	public override string Texture => $"DingDongBiomeHomes/Items/MobRelics/Generated/{internalName}";
	public override LocalizedText DisplayName => Language.GetOrRegister($"Mods.DingDongBiomeHomes.Items.{internalName}.DisplayName", () => displayName);
	public override LocalizedText Tooltip => Language.GetOrRegister($"Mods.DingDongBiomeHomes.MobRelics.RuntimeItems.{internalName}.Tooltip", () => string.Empty);

	internal GeneratedMobRelicItem(MobRelicSourceDefinition source, MobRelicTier tier, int mechanicOrdinal)
	{
		this.source = source;
		this.tier = tier;
		internalName = $"MobRelic_{source.StableId}_{(byte)tier}";
		displayName = MobRelicNaming.DisplayName(source, tier);
		profile = MobRelicMechanics.CreateProfile(source, tier, mechanicOrdinal);
		visualIdentity = MobRelicVisualIdentity.Create(source, Texture);
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.damage = 0;
		Item.shoot = ProjectileID.None;
		Item.useStyle = ItemUseStyleID.None;
		Item.maxStack = 1;
		Item.accessory = true;
		Item.material = false;
		Item.consumable = false;
		Item.value = tier switch
		{
			MobRelicTier.CommonTrace => Item.sellPrice(silver: 1),
			MobRelicTier.CommonCurio => Item.sellPrice(silver: 3),
			MobRelicTier.ExpertSeal => Item.sellPrice(silver: 8),
			MobRelicTier.MasterRelic => Item.sellPrice(silver: 20),
			MobRelicTier.LegendaryMyth => Item.sellPrice(gold: 1),
			_ => 0
		};
		Item.rare = tier switch
		{
			MobRelicTier.CommonTrace => ItemRarityID.Blue,
			MobRelicTier.CommonCurio => ItemRarityID.Green,
			MobRelicTier.ExpertSeal => ItemRarityID.Expert,
			MobRelicTier.MasterRelic => ItemRarityID.Master,
			MobRelicTier.LegendaryMyth => ItemRarityID.Red,
			_ => ItemRarityID.White
		};
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		// Remove only legacy mechanics-key leaks before adding live player text.
		tooltips.RemoveAll(line => line.Text.StartsWith("Mods.DingDongBiomeHomes.MobRelics.Mechanics.", StringComparison.Ordinal));
		// Dynamic items resolve localized mechanics only when the tooltip is requested.
		string drop = MobRelicNaming.PlayerText(MobRelicMechanics.DropSummary(source, tier), $"{tier} unique drop from {source.DisplayName} ({source.Provider}).");
		string effect = MobRelicNaming.PlayerText(MobRelicMechanics.EffectSummary(profile), $"Equippable {tier} relic with a generated combat effect.");
		string synergy = MobRelicNaming.PlayerText(MobRelicMechanics.SynergySummary(source), $"Combine matching relic tiers from {source.DisplayName}; relics from several {source.Provider} enemies grant Provider Harmony.");
		tooltips.Add(new TooltipLine(Mod, "MobRelicDrop", drop));
		tooltips.Add(new TooltipLine(Mod, "MobRelicEffect", effect));
		tooltips.Add(new TooltipLine(Mod, "MobRelicSynergy", synergy));
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		player.GetModPlayer<MobRelicPlayer>().Equip(profile);
		if (!hideVisual)
			player.GetModPlayer<MobRelicVisualPlayer>().RegisterVisible(Type, tier);
	}

}
