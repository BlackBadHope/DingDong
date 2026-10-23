using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Climate;

internal static class ContinentalClimatePolicy
{
	internal const int MaximumExposure = 3600;
	internal const int DebuffThreshold = 1800;
	internal const int NormalRecovery = 10;
	internal const int ForestRecovery = 40;
	internal const int BuffDuration = 180;
	internal const int BuffRefreshInterval = 60;

	internal static ContinentalClimateRegion ResolveRegion(Player player)
	{
		if (!player.ZoneOverworldHeight)
			return ContinentalClimateRegion.Neutral;
		if (player.ZoneSnow)
			return ContinentalClimateRegion.Arctic;
		if (player.ZoneDesert)
			return ContinentalClimateRegion.Sahara;
		if (player.ZoneJungle)
			return ContinentalClimateRegion.Amazon;
		return player.ZoneForest ? ContinentalClimateRegion.Forest : ContinentalClimateRegion.Neutral;
	}

	internal static int HeatDelta(Player player)
	{
		if (player.wet)
			return -20;
		int delta = Main.dayTime ? 2 : 1;
		if (player.ZoneSandstorm)
			delta += 2;
		if (player.HasBuff(BuffID.Campfire))
			delta += 1;
		if (player.HasBuff(BuffID.ObsidianSkin) || player.fireWalk || player.lavaImmune)
			delta -= 4;
		return delta;
	}

	internal static int ColdDelta(Player player)
	{
		int delta = Main.dayTime ? 2 : 3;
		if (Main.raining)
			delta += 2;
		if (player.wet)
			delta += 3;
		if (player.resistCold || player.HasBuff(BuffID.Warmth))
			delta -= 12;
		if (player.HasBuff(BuffID.Campfire))
			delta -= 8;
		return delta;
	}

	internal static int JungleDelta(Player player)
	{
		int delta = 2;
		if (Main.raining)
			delta += 1;
		if (player.wet)
			delta += 2;
		if (player.HasBuff(BuffID.Campfire))
			delta -= 6;
		return delta;
	}

	internal static int Step(int current, int delta) =>
		Math.Clamp(current + delta, 0, MaximumExposure);
}
