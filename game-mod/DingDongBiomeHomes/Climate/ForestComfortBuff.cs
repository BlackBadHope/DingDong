using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed class ForestComfortBuff : ModBuff
{
	public override string Texture => "DingDongBiomeHomes/Climate/Buffs/ForestComfortBuff";

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.moveSpeed += 0.04f;
		player.lifeRegen += 2;
		player.pickSpeed = Math.Max(0.35f, player.pickSpeed - 0.05f);
	}
}
