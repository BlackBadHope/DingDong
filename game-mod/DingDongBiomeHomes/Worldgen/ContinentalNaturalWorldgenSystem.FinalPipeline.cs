using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

public sealed partial class ContinentalNaturalWorldgenSystem
{
	private Hook finalPipelineHook;
	private delegate void ModifyTasks(List<GenPass> tasks, ref double weight);
	private delegate void ModifyTasksHook(ModifyTasks original, List<GenPass> tasks, ref double weight);

	public override void Load()
	{
		var method = typeof(SystemLoader).GetMethod("ModifyWorldGenTasks",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			null, [typeof(List<GenPass>), typeof(double).MakeByRefType()], null)
			?? throw new MissingMethodException("SystemLoader.ModifyWorldGenTasks");
		finalPipelineHook = new Hook(method, (ModifyTasksHook)AfterAllProviders);
		Mod.Logger.Info("DINGDONG_NATIVE_PIPELINE_HOOK|installed=True|boundary=after-all-ModifyWorldGenTasks");
	}

	private void AfterAllProviders(ModifyTasks original, List<GenPass> tasks, ref double weight)
	{
		// Providers must see their original pass objects, not wrappers of those
		// objects. Arm against the final list after all providers and filtering.
		original(tasks, ref weight);
		Fishing.Worldgen.FishingWaterWorldSystem.Attach(tasks, ref weight);
		OceanExpeditions.OceanExpeditionWorldSystem.Attach(tasks, ref weight);
		// V03 29.10: after both sea passes exist, so the coast seal follows "DingDong open ocean".
		Fishing.Worldgen.FishingWaterWorldSystem.AttachShoreSeal(tasks, ref weight);
		BiomeHomeWorldgenSystem.FinalizeResidencePass(Mod, tasks);
		AttachFinalNativePipeline(tasks, ref weight);
	}

	public override void Unload()
	{
		finalPipelineHook?.Dispose();
		finalPipelineHook = null;
	}
}
