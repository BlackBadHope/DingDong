using System;
using System.Threading.Tasks;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerExecutionPolicy
{
	private const int WorkerCeiling = 8;
	internal static int Workers { get; } = ComputeWorkers();
	internal static ParallelOptions ParallelOptions { get; } = new()
	{
		MaxDegreeOfParallelism = Workers
	};

	private static int ComputeWorkers()
	{
		int cpuBudget = Math.Max(1, Environment.ProcessorCount - 2);
		return Math.Min(cpuBudget, WorkerCeiling);
	}
}
