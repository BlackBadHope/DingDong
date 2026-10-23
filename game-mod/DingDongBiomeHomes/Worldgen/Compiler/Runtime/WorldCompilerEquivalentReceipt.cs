namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly record struct WorldCompilerEquivalentReceipt(
	int SpawnSanctuaries, int DungeonRooms, int TempleRooms,
	int DesertPyramids, int MushroomCaverns)
{
	internal bool Complete => SpawnSanctuaries > 0 && DungeonRooms > 0 &&
		TempleRooms > 0 && DesertPyramids > 0 && MushroomCaverns > 0;
	internal string Value => $"spawn={SpawnSanctuaries},dungeon={DungeonRooms}," +
		$"temple={TempleRooms},desert={DesertPyramids},mushroom={MushroomCaverns}";
}
