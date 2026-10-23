using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class StardustWelcomeCord : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Summon, AiArsenalTier.Lunar, 105, 24, 5.5f, 0, ItemRarityID.Red, Item.sellPrice(gold: 12), ProjectileID.RainbowWhip, 6f);
}
