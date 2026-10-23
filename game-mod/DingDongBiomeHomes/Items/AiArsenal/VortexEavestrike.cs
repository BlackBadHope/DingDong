using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class VortexEavestrike : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Ranged, AiArsenalTier.Lunar, 88, 9, 3.5f, 10, ItemRarityID.Red, Item.sellPrice(gold: 12), ProjectileID.Bullet, 16f, AmmoID.Bullet);
}
