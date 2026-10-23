using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class TwinebellLash : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Summon, AiArsenalTier.Timber, 14, 32, 3.5f, 0, ItemRarityID.Blue, Item.sellPrice(silver: 45), ProjectileID.BlandWhip, 4f);
}
