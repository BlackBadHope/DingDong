using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

public sealed class ContinentalSectionGuardPlayer : ModPlayer
{
	private const int CollisionPaddingPixels = 48;
	private const int CollisionPredictionTicks = 8;
	private Vector2 lastSafePosition;
	private bool hasSafePosition;
	private int stalledTicks;

	public override void OnEnterWorld() => ResetGuard();
	public override void UpdateDead() => ResetGuard();

	public override void PreUpdateMovement()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient ||
			Player.whoAmI != Main.myPlayer ||
			!ContinentalWorldDimensions.IsContinental)
			return;
		Vector2 displacement = Player.position - lastSafePosition;
		if (hasSafePosition && ContinentalSectionGuardPolicy.RebaseAfterDiscontinuity(displacement.X, displacement.Y, Player.velocity.Length()))
		{
			// A legitimate teleport invalidates the old anchor. Still freeze until
			// destination tiles arrive; never pull the player across the world.
			hasSafePosition = false;
			stalledTicks = 0;
		}
		if (SectionsReady(Player.position, Player.velocity, out int missingX,
			out int missingY))
		{
			if (stalledTicks >= 30)
				Mod.Logger.Info($"DINGDONG_SECTION_RESUMED|waitTicks={stalledTicks}");
			lastSafePosition = Player.position;
			hasSafePosition = true;
			stalledTicks = 0;
			RequestMissingVisibleSection();
			return;
		}
		stalledTicks++;
		if (ContinentalSectionGuardPolicy.RequestAt(stalledTicks))
			ContinentalSectionStreamingNetwork.RequestSection(missingX, missingY, urgent: true);
		if (stalledTicks == 30)
		{
			Main.NewText(Language.GetTextValue("Mods.DingDongBiomeHomes.WorldStreaming.Waiting"), Color.Goldenrod);
			Mod.Logger.Warn($"DINGDONG_SECTION_WAIT|sectionX={missingX}|sectionY={missingY}|waitTicks={stalledTicks}");
		}
		if (hasSafePosition)
			Player.position = lastSafePosition;
		Player.velocity = Vector2.Zero;
		Player.fallStart = (int)(Player.position.Y / 16f);
		Player.controlDown = false;
		Player.controlJump = false;
		Player.controlLeft = false;
		Player.controlRight = false;
		Player.controlUp = false;
	}

	private static void RequestMissingVisibleSection()
	{
		if (Main.GameUpdateCount % ContinentalSectionRequestPolicy.MinimumRequestInterval != 0)
			return;
		const int viewportPaddingPixels = 64;
		int minX = SectionX(Main.screenPosition.X - viewportPaddingPixels);
		int maxX = SectionX(Main.screenPosition.X + Main.screenWidth + viewportPaddingPixels);
		int minY = SectionY(Main.screenPosition.Y - viewportPaddingPixels);
		int maxY = SectionY(Main.screenPosition.Y + Main.screenHeight + viewportPaddingPixels);
		for (int x = minX; x <= maxX; x++)
			for (int y = minY; y <= maxY; y++)
				if (!Main.sectionManager.SectionLoaded(x, y) &&
					ContinentalSectionStreamingNetwork.RequestSection(x, y))
				{
					return;
				}
	}

	private bool SectionsReady(Vector2 position, Vector2 velocity, out int missingX,
		out int missingY)
	{
		missingX = -1;
		missingY = -1;
		Vector2 predicted = position + velocity * CollisionPredictionTicks;
		float left = Math.Min(position.X, predicted.X) - CollisionPaddingPixels;
		float top = Math.Min(position.Y, predicted.Y) - CollisionPaddingPixels;
		float right = Math.Max(position.X, predicted.X) + Player.width +
			CollisionPaddingPixels;
		float bottom = Math.Max(position.Y, predicted.Y) + Player.height +
			CollisionPaddingPixels;
		int minX = SectionX(left);
		int maxX = SectionX(right);
		int minY = SectionY(top);
		int maxY = SectionY(bottom);
		for (int x = minX; x <= maxX; x++)
			for (int y = minY; y <= maxY; y++)
				if (!Main.sectionManager.SectionLoaded(x, y))
				{
					missingX = x;
					missingY = y;
					return false;
				}
		return true;
	}

	private static int SectionX(float pixels)
	{
		int tile = Math.Clamp((int)Math.Floor(pixels / 16f), 0, Main.maxTilesX - 1);
		return Netplay.GetSectionX(tile);
	}

	private static int SectionY(float pixels)
	{
		int tile = Math.Clamp((int)Math.Floor(pixels / 16f), 0, Main.maxTilesY - 1);
		return Netplay.GetSectionY(tile);
	}

	private void ResetGuard()
	{
		lastSafePosition = Vector2.Zero;
		hasSafePosition = false;
		stalledTicks = 0;
	}
}
