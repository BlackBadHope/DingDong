using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class CinderChimneyCleaver : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Melee, AiArsenalTier.Underworld, 43, 28, 6.5f, 4, ItemRarityID.Orange, Item.sellPrice(gold: 3));
}
