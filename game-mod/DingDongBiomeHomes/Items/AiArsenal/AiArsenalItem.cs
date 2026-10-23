using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public abstract class AiArsenalItem : ModItem
{
	internal abstract AiArsenalSpec Spec { get; }
	public override string Texture => $"DingDongBiomeHomes/Items/AiArsenal/{Name}";
	internal AiArsenalCategory Category => Spec.Category;
	internal AiArsenalTier Tier => Spec.Tier;

	public override void SetDefaults()
	{
		AiArsenalSpec spec = Spec;
		if (spec.Category == AiArsenalCategory.Summon)
		{
			Item.DefaultToWhip(spec.ProjectileType, spec.Damage, spec.Knockback, spec.ShootSpeed, spec.UseTime);
		}
		else
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = spec.Damage;
			Item.knockBack = spec.Knockback;
			Item.crit = spec.Crit;
			Item.useTime = spec.UseTime;
			Item.useAnimation = spec.UseTime;
			Item.autoReuse = spec.Tier >= AiArsenalTier.Hallowed;
			Item.shoot = spec.ProjectileType;
			Item.shootSpeed = spec.ShootSpeed;
			Item.useAmmo = spec.AmmoType;
			Item.mana = spec.Mana;
			switch (spec.Category)
			{
				case AiArsenalCategory.Melee:
					Item.DamageType = DamageClass.Melee;
					Item.useStyle = ItemUseStyleID.Swing;
					Item.useTurn = true;
					Item.UseSound = SoundID.Item1;
					break;
				case AiArsenalCategory.Ranged:
					Item.DamageType = DamageClass.Ranged;
					Item.useStyle = ItemUseStyleID.Shoot;
					Item.noMelee = true;
					Item.UseSound = spec.AmmoType == AmmoID.Arrow ? SoundID.Item5 : SoundID.Item11;
					break;
				case AiArsenalCategory.Magic:
					Item.DamageType = DamageClass.Magic;
					Item.useStyle = ItemUseStyleID.Shoot;
					Item.noMelee = true;
					Item.UseSound = SoundID.Item20;
					break;
			}
		}

		Item.rare = spec.Rarity;
		Item.value = spec.Value;
	}

	public override void AddRecipes()
	{
		switch (Spec.Tier)
		{
			case AiArsenalTier.Timber:
				CreateRecipe().AddIngredient(ItemID.Wood, 15).AddIngredient(ItemID.Torch, 5).AddIngredient(ItemID.FallenStar).AddTile(TileID.WorkBenches).Register();
				break;
			case AiArsenalTier.Gilded:
				AddGildedRecipe(ItemID.GoldBar);
				AddGildedRecipe(ItemID.PlatinumBar);
				break;
			case AiArsenalTier.Underworld:
				CreateRecipe().AddIngredient(ItemID.HellstoneBar, 12).AddIngredient(ItemID.Obsidian, 8).AddTile(TileID.Hellforge).Register();
				break;
			case AiArsenalTier.Hallowed:
				CreateRecipe().AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.CrystalShard, 8).AddTile(TileID.MythrilAnvil).Register();
				break;
			case AiArsenalTier.Lunar:
				CreateRecipe().AddIngredient(ItemID.LunarBar, 8).AddIngredient(LunarFragment(), 12).AddTile(TileID.LunarCraftingStation).Register();
				break;
		}
	}

	private void AddGildedRecipe(int barType)
	{
		CreateRecipe().AddIngredient(barType, 12).AddIngredient(ItemID.FallenStar, 2).AddTile(TileID.Anvils).Register();
	}

	private int LunarFragment() => Spec.Category switch
	{
		AiArsenalCategory.Melee => ItemID.FragmentSolar,
		AiArsenalCategory.Ranged => ItemID.FragmentVortex,
		AiArsenalCategory.Magic => ItemID.FragmentNebula,
		_ => ItemID.FragmentStardust
	};
}
