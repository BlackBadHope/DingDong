using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace DingDongBiomeHomes.OceanExpeditions;

public abstract class OceanBanner : ModItem
{
    protected abstract int Style { get; }
    public override string Texture => "Terraria/Images/Item_" + ItemID.SharkBanner;
    public override void SetStaticDefaults() => ItemID.Sets.KillsToBanner[Type] = 50;
    public override void SetDefaults()
    { Item.DefaultToPlaceableTile(ModContent.TileType<OceanBannerTile>(), Style); Item.width = 12; Item.height = 28; Item.rare = ItemRarityID.Blue; Item.value = Item.buyPrice(silver: 10); }
}
public sealed class OceanGiantBanner : OceanBanner { protected override int Style => 0; }
public sealed class GrottoBanner : OceanBanner { protected override int Style => 1; }
public sealed class ReefFaunaBanner : OceanBanner { protected override int Style => 2; }
public sealed class OceanBannerTile : ModBannerTile
{
    public override string Texture => "Terraria/Images/Tiles_" + TileID.Banners;
}
