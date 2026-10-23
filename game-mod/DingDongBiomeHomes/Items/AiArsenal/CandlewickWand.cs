using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class CandlewickWand : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Magic, AiArsenalTier.Timber, 14, 30, 3f, 4, ItemRarityID.Blue, Item.sellPrice(silver: 45), ProjectileID.WandOfSparkingSpark, 7f, Mana: 5);
}
