using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class HallowedHearthbrand : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Melee, AiArsenalTier.Hallowed, 68, 20, 6f, 8, ItemRarityID.Pink, Item.sellPrice(gold: 6));
}
