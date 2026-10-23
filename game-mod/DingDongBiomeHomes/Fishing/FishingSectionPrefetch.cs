using System;
using System.Diagnostics;
using DingDongBiomeHomes.Worldgen;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Fishing;

// Uses the existing ACK/window and shared send budget on continental worlds.
internal static class FishingSectionPrefetch
{
	internal static bool TrySend(int who, double milliseconds)
	{
		if (Main.netMode != NetmodeID.Server || who < 0 || who >= Main.maxPlayers ||
			!Main.player[who].active || Main.player[who].dead) return false;
		var sections = Netplay.Clients[who].TileSections;
		if (sections == null) return false;
		long started = Stopwatch.GetTimestamp();
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile p = Main.projectile[i];
			if (!p.active || !p.bobber || p.owner != who || !p.wet || p.ai[0] != 0) continue;
			var rig = p.GetGlobalProjectile<FishingBobber>().Rig(p);
			if (rig.TargetDepthTiles <= 0 || p.Distance(Main.player[who].Center) > (rig.TargetDepthTiles + 80) * 16) continue;
			int tileX = Math.Clamp((int)(p.Center.X / 16), 1, Main.maxTilesX - 2);
			int tileY = Math.Clamp((int)(p.Center.Y / 16), 1, Main.maxTilesY - 2);
			int sx = Netplay.GetSectionX(tileX), sy = Netplay.GetSectionY(tileY);
			int ahead = Netplay.GetSectionY(Math.Min(Main.maxTilesY - 2, tileY + 64));
			for (int y = sy; y <= ahead; y++)
				for (int dx = 0; dx < 3; dx++)
				{
					if (Stopwatch.GetElapsedTime(started).TotalMilliseconds >= milliseconds) return false;
					int x = sx + (dx == 0 ? 0 : dx == 1 ? -1 : 1);
					if (x < 0 || y < 0 || x >= sections.GetLength(0) || y >= sections.GetLength(1) || sections[x, y]) continue;
					if (ContinentalWorldDimensions.IsContinental)
						return ContinentalSectionStreamingNetwork.SendSection(who, x, y);
					NetMessage.SendSection(who, x, y);
					return sections[x, y];
				}
		}
		return false;
	}
}

public sealed class FishingStandardWorldPrefetch : ModSystem
{
	private int nextPlayer;
	public override void PostUpdatePlayers()
	{
		if (ContinentalWorldDimensions.IsContinental || Main.netMode != NetmodeID.Server || Main.GameUpdateCount % 20 != 0) return;
		for (int offset = 0; offset < Main.maxPlayers; offset++)
		{
			int who = (nextPlayer + offset) % Main.maxPlayers;
			if (!Main.player[who].active || !FishingSectionPrefetch.TrySend(who, 1)) continue;
			nextPlayer = (who + 1) % Main.maxPlayers;
			break;
		}
	}
}
