using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class BriarThresholdWhip : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Summon, AiArsenalTier.Gilded, 24, 30, 4f, 0, ItemRarityID.Green, Item.sellPrice(gold: 1), ProjectileID.ThornWhip, 4.5f);
}
