using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace DingDongBiomeHomes;

public sealed partial class DingDongHousingProbeCommand
{
	private static Rectangle FindDisposableEnvelope(int width, int height)
	{
		const int top = 40;
		for (int left = 80; left + width < Main.maxTilesX - 80; left += width + 40)
		{
			Rectangle candidate = new(left, top, width, height);
			bool hasTileEntity = TileEntity.ByPosition.Keys.Any(position => candidate.Contains(position.X, position.Y));
			bool hasChest = Main.chest.Any(chest => chest is not null && candidate.Contains(chest.x, chest.y));
			bool hasSign = Main.sign.Any(sign => sign is not null && candidate.Contains(sign.x, sign.y));
			if (!hasTileEntity && !hasChest && !hasSign)
				return candidate;
		}

		throw new InvalidOperationException($"No disposable housing-probe envelope fits world {Main.maxTilesX}x{Main.maxTilesY}.");
	}

	private static void ClearProbeArea(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
		{
			for (int y = area.Top; y < area.Bottom; y++)
				Main.tile[x, y].ClearEverything();
		}
	}

	private static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
}
