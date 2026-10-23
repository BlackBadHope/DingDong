using System;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public sealed class NightPressureGlobalNPC : GlobalNPC
{
	internal const float SurfaceSpawnMultiplier = 3f;
	internal const float UndergroundSpawnMultiplier = 1.5f;
	internal const float HostileContactDamageMultiplier = 1.5f;
	internal static bool ContractPass() =>
		SurfaceSpawnMultiplier == 3f && UndergroundSpawnMultiplier == 1.5f && HostileContactDamageMultiplier == 1.5f;

	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
	{
		if (Main.dayTime || !ModContent.GetInstance<DingDongServerConfig>().EnableNightPressure)
			return;

		float multiplier = player.ZoneOverworldHeight || player.ZoneSkyHeight ? SurfaceSpawnMultiplier : UndergroundSpawnMultiplier;
		if (multiplier <= 1f)
			return;

		spawnRate = Math.Max(1, (int)Math.Ceiling(spawnRate / multiplier));
		maxSpawns = Math.Max(1, (int)Math.Ceiling(maxSpawns * multiplier));
	}

	public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
	{
		if (!Main.dayTime && ModContent.GetInstance<DingDongServerConfig>().EnableNightPressure && !npc.friendly && npc.damage > 0)
			modifiers.SourceDamage *= HostileContactDamageMultiplier;
	}
}
