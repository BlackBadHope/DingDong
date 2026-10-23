using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly record struct WorldCompilerProgressionObject(string Name,
	int TileType, int Style, WorldCompilerRect Bounds, int MinimumCells);
internal readonly record struct WorldCompilerProgressionChest(string Kind, int Index,
	int X, int Y, int Primary, bool Locked);

internal sealed class WorldCompilerProgressionReceipt
{
	internal List<WorldCompilerProgressionObject> Objects { get; } = [];
	internal List<WorldCompilerProgressionChest> Chests { get; } = [];
	internal int ValidatedPaths { get; set; }
	internal int NativePrimaryRestorations { get; set; }
	internal bool Complete { get; set; }
	internal bool PyramidLootPending { get; set; }
}
