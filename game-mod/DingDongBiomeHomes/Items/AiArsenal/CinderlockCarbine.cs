using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class CinderlockCarbine : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Ranged, AiArsenalTier.Underworld, 34, 18, 3f, 4, ItemRarityID.Orange, Item.sellPrice(gold: 3), ProjectileID.Bullet, 11f, AmmoID.Bullet);
}
