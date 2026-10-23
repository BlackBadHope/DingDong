using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerPhaseGraph
{
	internal static WorldCompilerPhase[] DefaultOrder() => Sort(
	[
		new(WorldCompilerPhase.MacroPlan),
		new(WorldCompilerPhase.BaseTerrain, WorldCompilerPhase.MacroPlan),
		new(WorldCompilerPhase.ClimateStrata, WorldCompilerPhase.BaseTerrain),
		new(WorldCompilerPhase.CavernGraph, WorldCompilerPhase.BaseTerrain),
		new(WorldCompilerPhase.Hydrology, WorldCompilerPhase.ClimateStrata,
			WorldCompilerPhase.CavernGraph),
		new(WorldCompilerPhase.Structures, WorldCompilerPhase.ClimateStrata,
			WorldCompilerPhase.CavernGraph),
		new(WorldCompilerPhase.NaturalDecoration, WorldCompilerPhase.Hydrology,
			WorldCompilerPhase.Structures),
		new(WorldCompilerPhase.Compatibility, WorldCompilerPhase.NaturalDecoration),
		new(WorldCompilerPhase.Finalize, WorldCompilerPhase.Compatibility),
		new(WorldCompilerPhase.Receipt, WorldCompilerPhase.Finalize)
	]);

	internal static WorldCompilerPhase[] Sort(WorldCompilerPhaseDescriptor[] phases)
	{
		var result = new List<WorldCompilerPhase>(phases.Length);
		var placed = new HashSet<WorldCompilerPhase>();
		for (int pass = 0; pass < phases.Length; pass++)
		{
			bool advanced = false;
			foreach (WorldCompilerPhaseDescriptor phase in phases)
			{
				if (placed.Contains(phase.Id) || !Ready(phase, placed)) continue;
				placed.Add(phase.Id);
				result.Add(phase.Id);
				advanced = true;
			}
			if (!advanced && result.Count < phases.Length)
				throw new InvalidOperationException("World compiler phase graph is cyclic.");
		}
		if (result.Count != phases.Length)
			throw new InvalidOperationException("World compiler phase IDs are duplicated.");
		return result.ToArray();
	}

	private static bool Ready(WorldCompilerPhaseDescriptor phase,
		HashSet<WorldCompilerPhase> placed)
	{
		foreach (WorldCompilerPhase dependency in phase.Dependencies)
			if (!placed.Contains(dependency)) return false;
		return true;
	}
}
