using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class EscortPathfinder
{
	private static bool ArcClear(NPC npc, Point from, Point to)
	{
		Vector2 start = new(from.X * 16f + 8f - npc.width * 0.5f, from.Y * 16f - npc.height);
		Vector2 end = new(to.X * 16f + 8f - npc.width * 0.5f, to.Y * 16f - npc.height);
		for (int step = 1; step < 5; step++)
		{
			float amount = step / 5f;
			Vector2 sample = Vector2.Lerp(start, end, amount);
			sample.Y -= (float)Math.Sin(Math.PI * amount) * 48f;
			if (Collision.SolidCollision(sample, npc.width, npc.height))
				return false;
		}

		return true;
	}
}
