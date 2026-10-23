using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class GildedGableGlaive : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Melee, AiArsenalTier.Gilded, 25, 22, 5f, 6, ItemRarityID.Green, Item.sellPrice(gold: 1));
}
