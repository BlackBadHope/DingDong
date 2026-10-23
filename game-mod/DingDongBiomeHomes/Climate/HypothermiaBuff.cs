using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed class HypothermiaBuff : ModBuff
{
	public override string Texture => "DingDongBiomeHomes/Climate/Buffs/HypothermiaBuff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.pickSpeed += 0.18f;
		player.jumpSpeedBoost -= 1.2f;
	}
}
