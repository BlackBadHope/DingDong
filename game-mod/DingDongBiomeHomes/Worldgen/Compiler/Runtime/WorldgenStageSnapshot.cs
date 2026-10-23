using System.Collections.Generic;
using System;
using System.Text;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldgenStageSnapshot
{
	internal static StageObservation[] Read()
	{
		var result = new List<StageObservation>();
		var receipt = WorldCompilerRuntimeState.Receipt?.Progression;
		if (receipt == null) return result.ToArray();
		foreach (var entry in receipt.Objects)
		{
			try {
			var b = entry.Bounds;
			var details = new StringBuilder();
			int cells = 0;
			bool locked = true;
			for (int y = b.Y; y < b.Bottom; y++)
			for (int x = b.X; x < b.Right; x++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == entry.TileType) cells++;
				if (entry.Name == "temple-locked-door") locked &= tile.HasTile && WorldGen.IsLockedDoor(tile);
				details.Append($"[{x},{y}:active={tile.HasTile},type={tile.TileType},frame={tile.TileFrameX},{tile.TileFrameY},liquid={tile.LiquidType}/{tile.LiquidAmount}]");
			}
			// Support tiles help distinguish an overwritten object from framing loss.
			for (int x = b.X; x < b.Right; x++)
			{
				Tile floor = Main.tile[x, b.Bottom];
				details.Append($"[support={x},{b.Bottom}:active={floor.HasTile},type={floor.TileType},slope={floor.Slope},half={floor.IsHalfBlock}]");
			}
			result.Add(new($"object:{entry.Name}:{b.X},{b.Y}",
				cells == entry.MinimumCells && locked,
				$"expectedType={entry.TileType};cells={cells}/{entry.MinimumCells};locked={locked};{details}"));
			} catch (Exception error) { result.Add(new($"reader:object:{entry.Name}:{entry.Bounds.X},{entry.Bounds.Y}", false, error.ToString())); }
		}
		foreach (var entry in receipt.Chests)
		{
			try {
			int index = Chest.FindChest(entry.X, entry.Y);
			var chest = index < 0 ? null : Main.chest[index];
			int primary = chest?.item[0]?.type ?? 0, stack = chest?.item[0]?.stack ?? 0;
			bool locked = chest != null && Chest.IsLocked(entry.X, entry.Y);
			bool physical = WorldCompilerChestIntegrity.Present(entry.X, entry.Y);
			var inventory = new StringBuilder();
			var matchingSlots = new List<int>();
			if (chest != null)
				for (int slot = 0; slot < chest.item.Length; slot++)
				{
					var item = chest.item[slot];
					inventory.Append($"[{slot}:type={item?.type},stack={item?.stack},prefix={item?.prefix},id={item?.ModItem?.FullName ?? item?.type.ToString()}]");
					if (item != null && item.type == entry.Primary && item.stack > 0) matchingSlots.Add(slot);
				}
			result.Add(new($"chest:{entry.Kind}:{entry.X},{entry.Y}",
				physical && chest != null && WorldCompilerChestContents.FindPrimary(chest, entry.Primary) >= 0 && locked == entry.Locked,
				$"index={index};physical={physical};primary={primary}/{entry.Primary};stack={stack};locked={locked}/{entry.Locked};expectedItemSlots={string.Join(",", matchingSlots)};inventory={inventory}" +
				(physical ? "" : WorldCompilerChestReadbackDetail.Describe(entry.X, entry.Y))));
			} catch (Exception error) { result.Add(new($"reader:chest:{entry.X},{entry.Y}", false, error.ToString())); }
		}
		WorldgenStageContracts.Append(result, receipt);
		return result.ToArray();
	}
}
