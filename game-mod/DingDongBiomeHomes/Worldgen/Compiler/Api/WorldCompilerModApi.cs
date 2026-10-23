using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Compiler.Api;

internal static class WorldCompilerModApi
{
	internal static bool TryHandle(object[] args, out object result)
	{
		result = null;
		if (args?.Length < 1 || args[0] is not string operation ||
			!operation.StartsWith("Worldgen.", StringComparison.Ordinal)) return false;
		result = operation switch
		{
			"Worldgen.GetCapabilities" when args.Length == 1 => Capabilities(),
			"Worldgen.GetPlanSummary" when args.Length == 1 => PlanSummary(),
			"Worldgen.GetBiomeBands" when args.Length == 1 => BiomeBands(),
			"Worldgen.GetContractSnapshot" when args.Length == 1 =>
				ContractSnapshot(),
			"Worldgen.GetSurfaceY" when args.Length == 2 && args[1] is int x =>
				SurfaceY(x),
			"Worldgen.RegisterAction" when args.Length == 5 &&
				args[1] is string provider && args[2] is string key &&
				args[3] is string phase && args[4] is Action callback =>
				WorldCompilerExtensionRegistry.Register(provider, key, phase, callback),
			_ => throw new ArgumentException($"Invalid DingDong worldgen call: {operation}.",
				nameof(args))
		};
		return true;
	}

	private static Dictionary<string, object> Capabilities() => new()
	{
		["apiVersion"] = 2,
		["operations"] = new[] { "Worldgen.GetCapabilities",
			"Worldgen.GetPlanSummary", "Worldgen.GetBiomeBands",
			"Worldgen.GetContractSnapshot", "Worldgen.GetSurfaceY",
			"Worldgen.RegisterAction" },
		["extensionPhases"] = WorldCompilerExtensionRegistry.PhaseNames(),
		["workers"] = WorldCompilerExecutionPolicy.Workers,
		["planAvailable"] = WorldCompilerRuntimeState.TryGetPlanView(out _)
	};

	private static Dictionary<string, object> PlanSummary()
	{
		WorldCompilerPlanView plan = RequirePlan();
		return new Dictionary<string, object>
		{
			["available"] = true,
			["width"] = plan.Width,
			["height"] = plan.Height,
			["seed"] = plan.Seed,
			["fingerprint"] = plan.Fingerprint.ToString("X16"),
			["chunks"] = plan.Chunks.Length,
			["caveNodes"] = plan.CaveNodes,
			["structureRooms"] = plan.StructureRooms
		};
	}

	private static object[] BiomeBands()
	{
		WorldCompilerPlanView plan = RequirePlan();
		var result = new object[plan.Bands.Length];
		for (int index = 0; index < result.Length; index++)
		{
			WorldCompilerBand band = plan.Bands[index];
			result[index] = new Dictionary<string, object>
			{
				["biome"] = band.Biome.ToString(),
				["left"] = band.Left,
				["right"] = band.Right
			};
		}
		return result;
	}

	private static int SurfaceY(int x)
	{
		WorldCompilerPlanView plan = RequirePlan();
		if ((uint)x >= (uint)plan.Width)
			throw new ArgumentOutOfRangeException(nameof(x));
		return plan.Surface[x];
	}

	private static Dictionary<string, object> ContractSnapshot()
	{
		WorldCompilerPlanView plan = RequirePlan();
		var surface = new int[plan.Width];
		for (int x = 0; x < surface.Length; x++) surface[x] = plan.Surface[x];
		var chunks = new object[plan.Chunks.Length];
		for (int index = 0; index < chunks.Length; index++)
		{
			WorldCompilerChunk chunk = plan.Chunks[index];
			chunks[index] = new Dictionary<string, object>
			{
				["index"] = chunk.Index,
				["left"] = chunk.Left,
				["right"] = chunk.Right,
				["readLeft"] = chunk.ReadLeft,
				["readRight"] = chunk.ReadRight
			};
		}
		return new Dictionary<string, object>
		{
			["apiVersion"] = 2,
			["summary"] = PlanSummary(),
			["surfaceY"] = surface,
			["biomeBands"] = BiomeBands(),
			["chunks"] = chunks
		};
	}

	private static WorldCompilerPlanView RequirePlan()
	{
		if (WorldCompilerRuntimeState.TryGetPlanView(out WorldCompilerPlanView plan))
			return plan;
		throw new InvalidOperationException(
			"DingDong world compiler plan is not active.");
	}
}
