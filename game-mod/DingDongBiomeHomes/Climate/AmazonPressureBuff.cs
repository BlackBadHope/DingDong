using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed class AmazonPressureBuff : ModBuff
{
	public override string Texture => "DingDongBiomeHomes/Climate/Buffs/AmazonPressureBuff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.manaCost += 0.10f;
		player.GetDamage(DamageClass.Generic) -= 0.06f;
		player.GetKnockback(DamageClass.Generic) -= 0.15f;
	}
}
