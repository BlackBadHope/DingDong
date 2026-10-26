using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

public abstract class OceanMaterial : ModItem
{
    public override void SetDefaults() { Item.width = Item.height = 22; Item.maxStack = 9999; Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(silver: 12); }
}
public sealed class PelagicScale : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.SharkFin; }
public sealed class HadalLens : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.BlackPearl; }
public sealed class GrottoGeode : OceanMaterial { public override string Texture => "Terraria/Images/Item_" + ItemID.Geode; }

public sealed class IslandNavigator : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Compass;
    public override void SetDefaults() { Item.width = Item.height = 24; Item.accessory = true; Item.rare = ItemRarityID.Blue; Item.value = Item.sellPrice(silver: 25); }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.accDepthMeter = 1; player.accCompass = 1;
        if (player.wet) player.moveSpeed += .10f;
    }
}
public sealed class PelagicHarness : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.DivingGear;
    public override void SetDefaults() { Item.width = Item.height = 28; Item.accessory = true; Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold: 1); }
    public override void UpdateAccessory(Player player, bool hideVisual)
    { player.breathMax += 240; player.accFlipper = true; player.ignoreWater = true; }
    public override void AddRecipes() => CreateRecipe().AddIngredient<PelagicScale>(8).AddIngredient<BrineBladder>(6).AddIngredient<RibbonGill>(6).AddIngredient<SeaEssence>(20).AddRecipeGroup(RecipeGroupID.IronBar, 8).AddTile(TileID.Anvils).Register();
}
public sealed class GrottoLantern : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.MagicLantern;
    public override void SetDefaults() { Item.width = Item.height = 24; Item.accessory = true; Item.rare = ItemRarityID.Orange; Item.value = Item.sellPrice(gold: 1); }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        Lighting.AddLight(player.Center, new Vector3(.20f, .65f, .57f));
        if (OceanExpeditionWorldSystem.TryGetZone((int)player.Center.X / 16, (int)player.Center.Y / 16, out var zone) && zone == OceanExpeditionZone.AirGrotto)
        { player.nightVision = true; player.lifeRegen += 3; }
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<GrottoGeode>(6).AddIngredient<LuminousGel>(8).AddIngredient<GlassWing>(4).AddIngredient<SeaEssence>(12).AddIngredient(ItemID.Glass, 20).AddTile(TileID.WorkBenches).Register();
}
public sealed class PressureWard : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.NeptunesShell;
    public override void SetDefaults() { Item.width = Item.height = 28; Item.accessory = true; Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 3); }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (!player.wet) return;
        player.endurance += .06f; player.GetDamage(DamageClass.Generic) += .08f;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<HadalLens>(12).AddIngredient<VentCore>(8).AddIngredient<SeaEssence>(30).AddIngredient(ItemID.SoulofNight, 8).AddIngredient(ItemID.SoulofLight, 8).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class Tidebreaker : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Trident;
    public override void SetDefaults()
    {
        Item.width = Item.height = 40; Item.damage = 54; Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = 24; Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = true; Item.noUseGraphic = true;
        Item.knockBack = 5; Item.autoReuse = true; Item.UseSound = SoundID.Item1;
        Item.shoot = ModContent.ProjectileType<TidebreakerLance>(); Item.shootSpeed = 11;
        Item.rare = ItemRarityID.LightRed; Item.value = Item.sellPrice(gold: 2);
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient<PelagicScale>(12).AddIngredient<GrottoGeode>(8).AddIngredient(ItemID.Trident).AddTile(TileID.Anvils).Register();
}
public sealed class TidebreakerLance : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Trident;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 22; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 3; Projectile.timeLeft = 90; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 20;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Lighting.AddLight(Projectile.Center, new Vector3(.05f, .2f, .25f));
    }
}
