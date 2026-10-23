using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class HallowedRooftopRepeater : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Ranged, AiArsenalTier.Hallowed, 50, 14, 3f, 8, ItemRarityID.Pink, Item.sellPrice(gold: 6), ProjectileID.WoodenArrowFriendly, 12f, AmmoID.Arrow);
}
