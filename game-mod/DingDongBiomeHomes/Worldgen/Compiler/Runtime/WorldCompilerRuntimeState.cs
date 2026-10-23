namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerRuntimeState
{
	internal static WorldCompilerPlan Current { get; private set; }
	internal static WorldCompilerPlanView? Published { get; private set; }
	internal static WorldCompilerExecutionReceipt Receipt { get; private set; }
	internal static WorldCompilerRuntimeWorkIndex WorkIndex { get; private set; }

	internal static WorldCompilerPlan Prepare(int width, int height, ulong seed)
	{
		Current = WorldCompilerPlanBuilder.Build(width, height, seed);
		Published = null;
		WorkIndex = null;
		Receipt = new WorldCompilerExecutionReceipt();
		return Current;
	}

	internal static long PrepareCaveWork() => SetWork(
		WorldCompilerRuntimeWorkIndex.BuildCaves(RequirePlan()));

	internal static long PrepareResourceWork() => SetWork(
		WorldCompilerRuntimeWorkIndex.BuildResources(RequirePlan()));

	internal static long PrepareHydrologyWork() => SetWork(
		WorldCompilerRuntimeWorkIndex.BuildHydrology(RequirePlan()));

	internal static long PrepareStructureWork() => SetWork(
		WorldCompilerRuntimeWorkIndex.BuildStructures(RequirePlan()));

	internal static void ReleasePhaseWork() => WorkIndex = null;

	internal static void PublishAndReleasePlan()
	{
		Published = WorldCompilerPlanView.From(RequirePlan());
		Current = null;
		ReleaseGenerationScratch();
	}

	internal static bool TryGetPlanView(out WorldCompilerPlanView plan)
	{
		if (Current != null)
		{
			plan = WorldCompilerPlanView.From(Current);
			return true;
		}
		if (Published.HasValue)
		{
			plan = Published.Value;
			return true;
		}
		plan = default;
		return false;
	}

	internal static void Reset()
	{
		Current = null;
		Published = null;
		Receipt = null;
		WorkIndex = null;
		WorldCompilerEllipseProfile.Clear();
	}

	internal static void ReleaseGenerationScratch()
	{
		WorkIndex = null;
		WorldCompilerEllipseProfile.Clear();
	}

	private static WorldCompilerPlan RequirePlan() => Current ??
		throw new System.InvalidOperationException(
			"World compiler plan is unavailable.");

	private static long SetWork(WorldCompilerRuntimeWorkIndex work)
	{
		if (WorkIndex != null)
			throw new System.InvalidOperationException(
				"World compiler phase work overlapped.");
		WorkIndex = work;
		return work.PayloadBytes;
	}
}
