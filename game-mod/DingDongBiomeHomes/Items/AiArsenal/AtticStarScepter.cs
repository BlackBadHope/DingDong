using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class AtticStarScepter : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Magic, AiArsenalTier.Gilded, 24, 24, 3.5f, 6, ItemRarityID.Green, Item.sellPrice(gold: 1), ProjectileID.AmethystBolt, 8f, Mana: 7);
}
