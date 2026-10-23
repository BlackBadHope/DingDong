using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class FurnaceChain : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Summon, AiArsenalTier.Underworld, 41, 28, 4.5f, 0, ItemRarityID.Orange, Item.sellPrice(gold: 3), ProjectileID.FireWhip, 5f);
}
