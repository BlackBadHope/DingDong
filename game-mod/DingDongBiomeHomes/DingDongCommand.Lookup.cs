using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public sealed partial class DingDongCommand
{
	private static bool TryFindSafeReviewPosition(Rectangle area, int playerWidth, int playerHeight, out Vector2 position)
	{
		position = Vector2.Zero;
		int left = Math.Max(10, area.Left + 1);
		int right = Math.Min(Main.maxTilesX - 10, area.Right - 2);
		int top = Math.Max(10, area.Top + 2);
		int bottom = Math.Min(Main.maxTilesY - 10, area.Bottom - 1);
		if (left > right || top > bottom)
			return false;
		int centerX = Math.Clamp(area.Center.X, left, right);
		for (int floorY = top; floorY <= bottom; floorY++)
		{
			for (int offset = 0; offset <= right - left; offset++)
			{
				int x = offset == 0 ? centerX : (offset & 1) == 1 ? centerX - (offset + 1) / 2 : centerX + offset / 2;
				if (x < left || x > right)
					continue;
				Vector2 candidate = new(x * 16f + 8f - playerWidth * 0.5f, floorY * 16f - playerHeight);
				if (Collision.SolidCollision(candidate, playerWidth, playerHeight))
					continue;
				Vector2 groundProbe = new(candidate.X, candidate.Y + playerHeight);
				if (!Collision.SolidCollision(groundProbe, playerWidth, 4))
					continue;
				position = candidate;
				return true;
			}
		}

		return false;
	}
}
