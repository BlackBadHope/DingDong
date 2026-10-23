using System.Text;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerChestReadbackDetail
{
	internal static string Describe(int x, int y)
	{
		int index = Chest.FindChest(x, y);
		var result = new StringBuilder();
		for (int dx = 0; dx < 2; dx++)
		for (int dy = 0; dy < 3; dy++)
		{
			Tile tile = Main.tile[x + dx, y + dy];
			result.Append($"[tile={x+dx},{y+dy}:active={tile.HasTile},type={tile.TileType},frame={tile.TileFrameX},{tile.TileFrameY}]");
		}
		if (index < 0) return result.Append("missing-chest-record").ToString();
		Item[] items = Main.chest[index].item;
		for (int slot = 0; slot < items.Length; slot++)
		{
			Item item = items[slot];
			result.Append($"[{slot}:type={item.type},stack={item.stack},prefix={item.prefix}]");
		}
		return result.ToString();
	}
}
