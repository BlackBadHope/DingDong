using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class PrismaticBanisterLash : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Summon, AiArsenalTier.Hallowed, 65, 26, 5f, 0, ItemRarityID.Pink, Item.sellPrice(gold: 6), ProjectileID.MaceWhip, 5.5f);
}
