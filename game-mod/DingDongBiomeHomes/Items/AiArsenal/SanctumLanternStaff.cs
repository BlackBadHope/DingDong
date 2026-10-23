using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
public sealed class SanctumLanternStaff : AiArsenalItem
{
	internal override AiArsenalSpec Spec => new(AiArsenalCategory.Magic, AiArsenalTier.Hallowed, 58, 16, 4.5f, 8, ItemRarityID.Pink, Item.sellPrice(gold: 6), ProjectileID.CrystalStorm, 11f, Mana: 11);
}
