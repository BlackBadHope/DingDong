using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerProgressionProof
{
	internal static TagCompound Encode(WorldCompilerProgressionReceipt receipt,
		IReadOnlyList<WorldCompilerWalkPath> paths)
	{
		var objects = new List<TagCompound>();
		foreach (WorldCompilerProgressionObject entry in receipt.Objects)
			objects.Add(new TagCompound {
				["name"] = entry.Name, ["type"] = entry.TileType, ["style"] = entry.Style,
				["x"] = entry.Bounds.X, ["y"] = entry.Bounds.Y,
				["w"] = entry.Bounds.Width, ["h"] = entry.Bounds.Height,
				["cells"] = entry.MinimumCells,
				["hash"] = WorldCompilerProgressionHash.Tiles(entry.Bounds) });
		var chests = new List<TagCompound>();
		foreach (WorldCompilerProgressionChest entry in receipt.Chests)
			chests.Add(new TagCompound { ["kind"] = entry.Kind, ["x"] = entry.X,
				["y"] = entry.Y, ["primary"] = entry.Primary, ["locked"] = entry.Locked,
				["hash"] = WorldCompilerProgressionHash.Chest(entry.X, entry.Y) });
		var routes = new List<TagCompound>();
		foreach (WorldCompilerWalkPath path in paths)
			routes.Add(new TagCompound { ["ax"] = path.FromX, ["ay"] = path.FromY,
				["bx"] = path.ToX, ["by"] = path.ToY, ["vertical"] = path.VerticalFirst });
		return new TagCompound { ["version"] = 1, ["objects"] = objects,
			["chests"] = chests, ["paths"] = routes };
	}

	internal static void Validate(TagCompound proof)
	{
		if (proof.GetInt("version") != 1)
			throw new InvalidOperationException("Unsupported progression proof version.");
		var receipt = new WorldCompilerProgressionReceipt();
		var failures = new List<string>();
		foreach (TagCompound value in proof.GetList<TagCompound>("objects"))
		{
			var bounds = new WorldCompilerRect(value.GetInt("x"), value.GetInt("y"),
				value.GetInt("w"), value.GetInt("h"));
			if (WorldCompilerProgressionHash.Tiles(bounds) != value.GetString("hash"))
				failures.Add($"tile:{value.GetString("name")}:{bounds.X},{bounds.Y}:expected={value.GetString("hash")}:actual={WorldCompilerProgressionHash.Tiles(bounds)}");
			receipt.Objects.Add(new(value.GetString("name"), value.GetInt("type"),
				value.GetInt("style"), bounds, value.GetInt("cells")));
		}
		foreach (TagCompound value in proof.GetList<TagCompound>("chests"))
		{
			int x = value.GetInt("x"), y = value.GetInt("y");
			try
			{
				string actual = WorldCompilerProgressionHash.Chest(x, y);
				if (actual != value.GetString("hash"))
					failures.Add($"chest:{x},{y}:expected={value.GetString("hash")}:actual={actual}:" + WorldCompilerChestReadbackDetail.Describe(x, y));
			}
			catch (InvalidOperationException error) { failures.Add(error.Message + WorldCompilerChestReadbackDetail.Describe(x, y)); }
			receipt.Chests.Add(new(value.GetString("kind"), -1, x, y,
				value.GetInt("primary"), value.GetBool("locked")));
		}
		try { WorldCompilerProgressionValidation.ValidateObjects(receipt); }
		catch (InvalidOperationException error) { failures.Add(error.Message); }
		try { WorldCompilerProgressionValidation.ValidateChests(receipt); }
		catch (InvalidOperationException error) { failures.Add(error.Message); }
		foreach (TagCompound value in proof.GetList<TagCompound>("paths"))
		{
			var path = new WorldCompilerWalkPath(value.GetInt("ax"), value.GetInt("ay"),
				value.GetInt("bx"), value.GetInt("by"), value.GetBool("vertical"));
			if (!WorldCompilerPathContract.TryValidate(path,
				WorldCompilerProgressionValidation.Passable, out WorldCompilerPoint blocked))
				failures.Add($"path:{path.FromX},{path.FromY}->{path.ToX},{path.ToY}:blocked={blocked.X},{blocked.Y}");
		}
		if (failures.Count > 0)
			throw new InvalidOperationException($"Progression readback failures={failures.Count}:\n" + string.Join("\n", failures));
	}
}
