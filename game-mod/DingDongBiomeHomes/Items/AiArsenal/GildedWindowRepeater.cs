using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class GildedWindowRepeater : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Ranged, AiArsenalTier.Gilded, 22, 22, 2.5f, 6, ItemRarityID.Green, Item.sellPrice(gold: 1), ProjectileID.WoodenArrowFriendly, 8.5f, AmmoID.Arrow);
}
