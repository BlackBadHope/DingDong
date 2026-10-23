using System;
using System.Collections;
using Microsoft.Xna.Framework;
using DingDongBiomeHomes.Worldgen.Compiler;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static bool TryBuildCompilerEvilBiome()
	{
		WorldCompilerPlan plan = WorldCompilerRuntimeState.Current;
		if (!ContinentalAdapterMath.Active() || plan == null) return false;
		WorldCompilerInfection evil = FindCompilerEvil(plan);
		if (corruptionsField?.GetValue(null) is not IList corruptions)
			throw new InvalidOperationException("SOTS corruption list is unavailable.");

		int width = Math.Clamp(evil.HalfWidth * 2 - 120, 600, 1600);
		int left = Math.Clamp(evil.SurfaceX - width / 2, 200,
			plan.Width - width - 200);
		int averageHeight = plan.Surface[evil.SurfaceX];
		int top = Math.Clamp(averageHeight - 40, 100, plan.Height - 300);
		Rectangle envelope = new(left, top, width, 200);
		object entry = Activator.CreateInstance(corruptionRectangleType) ??
			throw new InvalidOperationException("SOTS corruption rectangle allocation failed.");
		corruptionRectangleField.SetValue(entry, envelope);
		corruptionAverageHeightField.SetValue(entry, averageHeight);
		corruptions.Add(entry);
		flattenEvilBiomeMethod.Invoke(null, [0]);
		SotsWorldgenReceipt.MarkCompilerEvilBridge();
		global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
			$"DINGDONG_SOTS_EVIL_BRIDGE|verdict=PASS|area={left},{top},{width},200" +
			$"|averageHeight={averageHeight}|kind={evil.Kind}|count={corruptions.Count}");
		return true;
	}

	private static WorldCompilerInfection FindCompilerEvil(WorldCompilerPlan plan)
	{
		foreach (WorldCompilerInfection infection in plan.Resources.Infections)
			if (infection.Kind != WorldCompilerInfectionKind.Hallow)
				return infection;
		throw new InvalidOperationException(
			"Compiler plan has no evil infection for the SOTS village.");
	}

	private static void ResetAbandonedVillageCompatibility()
	{
		flattenEvilBiomeMethod = null;
		corruptionRectangleType = null;
		corruptionRectangleField = null;
		corruptionAverageHeightField = null;
	}
}
