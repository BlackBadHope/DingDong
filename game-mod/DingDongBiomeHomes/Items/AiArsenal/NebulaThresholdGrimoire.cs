using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class NebulaThresholdGrimoire : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Magic, AiArsenalTier.Lunar, 92, 10, 5f, 10, ItemRarityID.Red, Item.sellPrice(gold: 12), ProjectileID.NebulaBlaze1, 13f, Mana: 15);
}
