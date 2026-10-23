using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class FiveTileJumpPlayer : ModPlayer
{
	internal const float VanillaBaseJumpSpeed = 5.01f;
	internal const float HalfHeightBaseJumpSpeed = 3.0106818f;
	internal const int BaseJumpTicks = 15;
	internal const float VanillaGravity = 0.4f;

	public override void PostUpdateRunSpeeds()
	{
		if (!ModContent.GetInstance<DingDongServerConfig>().EnableFiveTileBaseJump || Player.mount.Active)
			return;

		float divisor = 1f;
		if (Player.sticky)
			divisor *= 5f;
		if (Player.dazed)
			divisor *= 2f;

		// Terraria has already combined vanilla, accessory and mod bonuses here.
		// Replacing only the scaled vanilla contribution preserves every bonus on top.
		Player.jumpSpeed += (HalfHeightBaseJumpSpeed - VanillaBaseJumpSpeed) / divisor;
	}

	internal static float SimulateUnobstructedBaseApex()
	{
		float velocity = 0f;
		float displacement = 0f;
		// Native JumpMovement applies the initial impulse, then jumpHeight holds.
		for (int tick = 0; tick <= BaseJumpTicks; tick++)
		{
			velocity = -HalfHeightBaseJumpSpeed;
			velocity += VanillaGravity;
			displacement -= velocity;
		}

		while (velocity < 0f)
		{
			velocity += VanillaGravity;
			if (velocity < 0f)
				displacement -= velocity;
		}
		return displacement;
	}

	internal static bool ContractPass(out float apexPixels)
	{
		apexPixels = SimulateUnobstructedBaseApex();
		float vanillaApex = SimulateApex(VanillaBaseJumpSpeed);
		return Math.Abs(apexPixels - vanillaApex * 0.5f) <= 0.01f;
	}

	private static float SimulateApex(float jumpSpeed)
	{
		float velocity = 0f;
		float displacement = 0f;
		for (int tick = 0; tick <= BaseJumpTicks; tick++)
		{
			velocity = -jumpSpeed;
			velocity += VanillaGravity;
			displacement -= velocity;
		}
		while (velocity < 0f)
		{
			velocity += VanillaGravity;
			if (velocity < 0f)
				displacement -= velocity;
		}
		return displacement;
	}
}
