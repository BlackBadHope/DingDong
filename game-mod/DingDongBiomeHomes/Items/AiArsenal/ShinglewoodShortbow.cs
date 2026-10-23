using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class ShinglewoodShortbow : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Ranged, AiArsenalTier.Timber, 12, 28, 2f, 4, ItemRarityID.Blue, Item.sellPrice(silver: 45), ProjectileID.WoodenArrowFriendly, 6.5f, AmmoID.Arrow);
}
