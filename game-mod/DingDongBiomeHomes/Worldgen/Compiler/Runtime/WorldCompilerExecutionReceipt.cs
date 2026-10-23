namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal sealed class WorldCompilerExecutionReceipt
{
	internal WorldCompilerTerrainReceipt Terrain { get; set; }
	internal WorldCompilerCaveReceipt Caves { get; set; }
	internal WorldCompilerResourceReceipt Resources { get; set; }
	internal WorldCompilerHydrologyReceipt Hydrology { get; set; }
	internal WorldCompilerStructureReceipt Structures { get; set; }
	internal WorldCompilerDecorationReceipt Decorations { get; set; }
	internal int CompatibilityPasses { get; set; }
	internal WorldCompilerCompatibilityExecutionReceipt Compatibility { get; set; }
	internal WorldCompilerEquivalentReceipt Equivalents { get; set; }
	internal int ExtensionActions { get; set; }
	internal bool Finalized { get; set; }
	internal bool Complete { get; set; }
	internal WorldCompilerProgressionReceipt Progression { get; set; }
}
