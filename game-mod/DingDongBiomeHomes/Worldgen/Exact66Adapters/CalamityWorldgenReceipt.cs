namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

using global::DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class CalamityWorldgenReceipt
{
	internal static bool ScaleObserved { get; private set; }
	internal static bool DesertBudgetObserved { get; private set; }
	internal static bool DesertCleanupObserved { get; private set; }
	internal static bool MushroomBudgetObserved { get; private set; }
	internal static bool DungeonCapacityObserved { get; private set; }
	internal static bool DungeonBudgetObserved { get; private set; }
	internal static bool TempleScaleObserved { get; private set; }
	internal static bool TempleBaseRoomsObserved { get; private set; }
	internal static bool ArchiveLookupObserved { get; private set; }
	internal static int ArchiveInvalidTypesAvoided { get; private set; }
	internal static float TempleScale { get; private set; }
	internal static int TempleBaseRooms { get; private set; }
	internal static int RoomCapacity { get; private set; }
	internal static int JunctionCapacity { get; private set; }
	internal static int DesertAttemptsPerSide { get; private set; }
	internal static long DesertCleanupFrames { get; private set; }
	internal static int MushroomAttemptsPerBiome { get; private set; }
	internal static ContinentalDungeonWorkBudget DungeonBudget { get; private set; }
	internal static bool Complete => ScaleObserved && DesertBudgetObserved && DesertCleanupObserved && MushroomBudgetObserved && DungeonCapacityObserved &&
		DungeonBudgetObserved && TempleScaleObserved && TempleBaseRoomsObserved &&
		ArchiveLookupObserved;
	internal static bool CompilerComplete(WorldCompilerEquivalentReceipt equivalents) =>
		ScaleObserved && ArchiveLookupObserved && equivalents.Complete;
	internal static string Value =>
		$"calamity:sunkenSeaGeometry={(ScaleObserved ? "local-unchanged" : "pending")}," +
		$"desertAttempts={(DesertBudgetObserved ? DesertAttemptsPerSide.ToString() : "pending")};" +
		$"desertCleanup={(DesertCleanupObserved ? DesertCleanupFrames.ToString() : "pending")};" +
		$"mushroomAttempts={(MushroomBudgetObserved ? MushroomAttemptsPerBiome.ToString() : "pending")};" +
		$"dungeonBuffers={(DungeonCapacityObserved ? $"{RoomCapacity}/{JunctionCapacity}" : "pending")};" +
		$"dungeonBudget={(DungeonBudgetObserved ? BudgetValue() : "pending")};" +
		$"templeScale={(TempleScaleObserved ? TempleScale.ToString() : "pending")}," +
		$"templeBaseRooms={(TempleBaseRoomsObserved ? TempleBaseRooms.ToString() : "pending")};" +
		$"archiveLookup={(ArchiveLookupObserved ? $"safe/{ArchiveInvalidTypesAvoided}" : "pending")}";

	internal static void MarkScale() => ScaleObserved = true;
	internal static void MarkDesertBudget(int attemptsPerSide)
	{
		DesertBudgetObserved = true;
		DesertAttemptsPerSide = attemptsPerSide;
	}
	internal static void MarkMushroomBudget(int attemptsPerBiome)
	{
		MushroomBudgetObserved = true;
		MushroomAttemptsPerBiome = attemptsPerBiome;
	}
	internal static void MarkDesertCleanup(long frames)
	{
		DesertCleanupObserved = true;
		DesertCleanupFrames = frames;
	}
	internal static void MarkTempleScale(float scale)
	{
		TempleScaleObserved = true;
		TempleScale = scale;
	}
	internal static void MarkTempleBaseRooms(int rooms)
	{
		TempleBaseRoomsObserved = true;
		TempleBaseRooms = rooms;
	}
	internal static void MarkArchiveLookup(bool invalidType)
	{
		ArchiveLookupObserved = true;
		if (invalidType) ArchiveInvalidTypesAvoided++;
	}
	internal static void MarkDungeonCapacity(ContinentalDungeonBufferPlan plan,
		ContinentalDungeonWorkBudget budget)
	{
		DungeonCapacityObserved = true;
		DungeonBudgetObserved = true;
		RoomCapacity = plan.RoomCapacity;
		JunctionCapacity = plan.JunctionCapacity;
		DungeonBudget = budget;
	}

	private static string BudgetValue() =>
		$"{DungeonBudget.PrimaryHallBase:0}/{DungeonBudget.PitTrapAttempts}/" +
		$"{DungeonBudget.WallFeatureTarget}/{DungeonBudget.TrapTarget}/" +
		$"{DungeonBudget.LightTarget}/{DungeonBudget.PlatformTarget}/" +
		$"{DungeonBudget.GroundFurnitureAttempts:0}/" +
		$"{DungeonBudget.StairSegmentAttempts}";

	internal static void Reset()
	{
		ScaleObserved = DesertBudgetObserved = DesertCleanupObserved = MushroomBudgetObserved = DungeonCapacityObserved =
			DungeonBudgetObserved = TempleScaleObserved =
			TempleBaseRoomsObserved = ArchiveLookupObserved = false;
		RoomCapacity = 0;
		JunctionCapacity = 0;
		TempleScale = 0f;
		TempleBaseRooms = 0;
		DesertAttemptsPerSide = 0;
		DesertCleanupFrames = 0;
		MushroomAttemptsPerBiome = 0;
		ArchiveInvalidTypesAvoided = 0;
		DungeonBudget = default;
	}
}
