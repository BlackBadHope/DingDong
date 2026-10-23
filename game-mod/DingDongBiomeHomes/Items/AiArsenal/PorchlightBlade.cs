using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class PorchlightBlade : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Melee, AiArsenalTier.Timber, 14, 24, 4.5f, 4, ItemRarityID.Blue, Item.sellPrice(silver: 45));
}
