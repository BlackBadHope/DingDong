using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class SolarHomesteadGreatblade : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Melee, AiArsenalTier.Lunar, 118, 24, 8f, 10, ItemRarityID.Red, Item.sellPrice(gold: 12));
}
