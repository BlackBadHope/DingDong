namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerChestPrimaryPolicy
{
	// Native AddBuriedChest: FTW replaces a jungle primary with AngelStatue (52), 1/15.
	// These authored progression chests guarantee their chosen primary. No other mismatch is repaired.
	internal static bool Restore(string kind, int style, int actual, int expected, bool ftw) =>
		ftw && kind == "jungle" && style == 10 && actual == 52 && expected > 0 && expected != 52;
}
