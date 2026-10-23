using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed class HeatstrokeBuff : ModBuff
{
	public override string Texture => "DingDongBiomeHomes/Climate/Buffs/HeatstrokeBuff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.GetAttackSpeed(DamageClass.Generic) -= 0.08f;
		player.endurance -= 0.05f;
	}
}
