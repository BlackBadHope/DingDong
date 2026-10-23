using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Climate;

public sealed partial class ContinentalClimatePlayer
{
	public override void UpdateBadLifeRegen()
	{
		int damage = 0;
		if (Player.HasBuff(ModContent.BuffType<HeatstrokeBuff>()))
			damage += 10;
		if (Player.HasBuff(ModContent.BuffType<HypothermiaBuff>()))
			damage += 8;
		if (damage == 0)
			return;
		if (Player.lifeRegen > 0)
			Player.lifeRegen = 0;
		Player.lifeRegenTime = 0;
		Player.lifeRegen -= damage;
	}

	public override void PostUpdateRunSpeeds()
	{
		if (Player.HasBuff(ModContent.BuffType<HeatstrokeBuff>()))
			ScaleRun(0.90f, 0.85f);
		if (Player.HasBuff(ModContent.BuffType<HypothermiaBuff>()))
			ScaleRun(0.78f, 0.70f);
		if (Player.HasBuff(ModContent.BuffType<AmazonPressureBuff>()))
			ScaleRun(0.92f, 0.88f);
		if (Player.HasBuff(ModContent.BuffType<ForestComfortBuff>()))
			ScaleRun(1.04f, 1.05f);
	}

	private void ScaleRun(float speed, float acceleration)
	{
		Player.maxRunSpeed *= speed;
		Player.accRunSpeed *= speed;
		Player.runAcceleration *= acceleration;
	}
}
