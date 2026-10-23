using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes.Items.AccessoryEnchanting;
using DingDongBiomeHomes.Items.MobRelics;
using DingDongBiomeHomes.Items.SynergyArmor;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
internal static partial class DingDongMultiplayerAbProbe
{
	private static bool TryFindOpenPlatform(NPC npc, out Point16 platform)
	{
		int originX = Math.Clamp(Main.spawnTileX, 20, Main.maxTilesX - 20);
		int originY = Math.Clamp(Main.spawnTileY, 20, Main.maxTilesY - 20);
		for (int radius = 0; radius <= 120; radius++)
		{
			for (int y = originY - radius; y <= originY + radius; y++)
			{
				for (int x = originX - radius; x <= originX + radius; x++)
				{
					if (!WorldGen.InWorld(x, y, 10) || Math.Max(Math.Abs(x - originX), Math.Abs(y - originY)) != radius)
						continue;
					Vector2 candidate = new(x * 16f + 24f - npc.width * 0.5f, y * 16f - npc.height);
					if (!Collision.SolidCollision(candidate, npc.width, npc.height))
					{
						platform = new Point16(x, y);
						return true;
					}
				}
			}
		}

		platform = default;
		return false;
	}
}
