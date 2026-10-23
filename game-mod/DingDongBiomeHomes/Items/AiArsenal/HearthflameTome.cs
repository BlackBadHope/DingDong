using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class HearthflameTome : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Magic, AiArsenalTier.Underworld, 39, 20, 4f, 4, ItemRarityID.Orange, Item.sellPrice(gold: 3), ProjectileID.Flamelash, 9f, Mana: 9);
}
