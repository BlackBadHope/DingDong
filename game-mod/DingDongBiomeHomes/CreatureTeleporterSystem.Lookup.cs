using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class CreatureTeleporterSystem
{
	private static bool TryFindDestination(NPC npc, Point16 platform, out Vector2 destination)
	{
		float centerX = platform.X * 16f + 24f;
		float platformTop = platform.Y * 16f;
		for (int upwardTiles = 0; upwardTiles <= 8; upwardTiles++)
		{
			Vector2 candidate = new(centerX - npc.width * 0.5f, platformTop - npc.height - upwardTiles * 16f);
			Rectangle hitbox = new((int)candidate.X, (int)candidate.Y, npc.width, npc.height);
			if (hitbox.Left < 16 || hitbox.Right >= Main.maxTilesX * 16 - 16 || hitbox.Top < 16 || hitbox.Bottom >= Main.maxTilesY * 16 - 16)
			{
				continue;
			}

			if (!Collision.SolidCollision(candidate, npc.width, npc.height))
			{
				destination = candidate;
				return true;
			}
		}

		destination = default;
		return false;
	}
}
