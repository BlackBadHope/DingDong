using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

public sealed class SeaEssence : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.Waterleaf; }
public sealed class CoralClaw : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.Coral; }
public sealed class GlassFin : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.SharkFin; }
public sealed class RibbonGill : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.JellyfishNecklace; }
public sealed class LuminousGel : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.PinkGel; }
public sealed class BrineBladder : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.BottledWater; }
public sealed class SiltWhisker : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.Worm; }
public sealed class Sailbone : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.Bone; }
public sealed class VentCore : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.ObsidianRose; }
public sealed class SaltCrystal : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalShard; }
public sealed class GlassWing : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.ButterflyDust; }

public sealed class ReefwalkerBoots : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Flipper;
    public override void SetDefaults() { Item.width = Item.height = 24; Item.accessory = true; Item.rare = ItemRarityID.Green; Item.value = Item.sellPrice(gold:1); }
    public override void UpdateAccessory(Player player,bool hideVisual)
    { player.accFlipper = true; if(player.wet)player.moveSpeed += .15f; }
    public override void AddRecipes() => CreateRecipe().AddIngredient<CoralClaw>(8).AddIngredient<SaltCrystal>(6).AddIngredient<SeaEssence>(15).AddTile(TileID.Anvils).Register();
}
public sealed class SoundingPendant : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.DepthMeter;
    public override void SetDefaults() { Item.width = Item.height = 24; Item.accessory = true; Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold:1); }
    public override void UpdateAccessory(Player player,bool hideVisual)
    { player.accDepthMeter = 1; if(player.wet)player.findTreasure = true; }
    public override void AddRecipes() => CreateRecipe().AddIngredient<SiltWhisker>(8).AddIngredient<GrottoGeode>(6).AddIngredient<SeaEssence>(20).AddTile(TileID.Anvils).Register();
}
public sealed class SailboneBow : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Marrow;
    public override void SetDefaults()
    {
        Item.width = 26; Item.height = 46; Item.damage = 29; Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = 22; Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = true;
        Item.autoReuse = true; Item.UseSound = SoundID.Item5; Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.shootSpeed = 11; Item.useAmmo = AmmoID.Arrow; Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold:2);
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<Sailbone>(10).AddIngredient<GlassFin>(8).AddIngredient<SeaEssence>(24).AddTile(TileID.Anvils).Register();
}
