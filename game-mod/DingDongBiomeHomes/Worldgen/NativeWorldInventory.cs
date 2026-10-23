using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// One streaming scan. No world-sized buffers; facts, not synthetic anchors.
internal static class NativeWorldInventory
{
	internal static void Export()
	{
		long[] counts = new long[TileLoader.TileCount];
		for (int x = 0; x < Main.maxTilesX; x++)
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile) counts[tile.TileType]++;
			}
		List<object> tiles = new();
		for (int type = 0; type < counts.Length; type++)
			if (counts[type] > 0) tiles.Add(new {
				id = type, count = counts[type],
				name = TileLoader.GetTile(type)?.FullName ?? TileID.Search.GetName(type)
			});
		var chests = Main.chest.Where(c => c != null).ToArray();
		Dictionary<string, long> items = new();
		foreach (Chest chest in chests)
			foreach (Item item in chest.item)
				if (item != null && !item.IsAir)
				{
					string key = item.ModItem?.FullName ?? ItemID.Search.GetName(item.type);
					items[key] = items.GetValueOrDefault(key) + item.stack;
				}
		string directory = Path.Combine(Main.SavePath, "DingDongDiagnostics", "WorldInventory");
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json");
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			world = Main.ActiveWorldFileData.Path, metadataSeed = Main.ActiveWorldFileData.Seed,
			width = Main.maxTilesX, height = Main.maxTilesY, ftw = Main.getGoodWorld,
			chests = chests.Length, items, tiles, residenceCount = SettlementWorldState.Residences.Count,
			worldMutated = false, scope = "physical inventory; not per-structure or visual acceptance"
		}));
		Console.WriteLine("DINGDONG_WORLD_INVENTORY|path=" + path);
	}
}
