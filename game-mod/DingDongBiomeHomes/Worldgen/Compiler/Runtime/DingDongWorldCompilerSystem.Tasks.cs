using System;
using System.Collections.Generic;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using DingDongBiomeHomes.Worldgen.Compiler.Api;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

public sealed partial class DingDongWorldCompilerSystem
{
	public override void ModifyWorldGenTasks(List<GenPass> tasks,
		ref double totalWeight)
	{
		if (!WorldCompilerActivation.IsActive) return;
		if (WorldCompilerRuntimeState.Current == null)
		{
			Block(tasks, ref totalWeight, "plan-missing");
			return;
		}
		if (!Exact66WorldgenAdmission.TryAdmit(Mod, tasks, out _, out string admission))
		{
			Block(tasks, ref totalWeight, $"compatibility-admission:{admission}");
			return;
		}
		if (!WorldCompilerCompatibilityCollector.TryCollect(tasks,
			out List<GenPass> compatibility, out string compatibilityReceipt))
		{
			Block(tasks, ref totalWeight, $"compatibility-collection:{compatibilityReceipt}");
			return;
		}

		tasks.Clear();
		Receipt.Compatibility = new WorldCompilerCompatibilityExecutionReceipt(compatibility);
		Add(tasks, "DingDong Compiler: Base Terrain", GenerateTerrain, 2.0);
		Add(tasks, "DingDong Compiler: Cavern Graph", GenerateCaves, 1.0);
		Add(tasks, "DingDong Compiler: Resources", GenerateResources, 0.8);
		Add(tasks, "DingDong Compiler: Hydrology", GenerateHydrology, 0.6);
		Add(tasks, "DingDong Compiler: World Metadata", (_, _) => WorldCompilerWorldContracts.Prepare(Plan), 0.1);
		for (int index = 0; index < compatibility.Count; index++)
			if (WorldCompilerCompatibilityPhases.BeforeStructures(compatibility[index].Name))
				tasks.Add(WrapCompatibility(compatibility[index], index));
		Add(tasks, "DingDong Compiler: Structures", GenerateStructures, 1.0);
		Add(tasks, "DingDong Compiler: World Contracts", PrepareContracts, 0.1);
		for (int index = 0; index < compatibility.Count; index++)
			if (!WorldCompilerCompatibilityPhases.BeforeStructures(compatibility[index].Name))
				tasks.Add(WrapCompatibility(compatibility[index], index));
		WorldCompilerRuntimeState.Receipt.CompatibilityPasses = compatibility.Count;
		if (WorldCompilerExtensionRegistry.HasActions(
			WorldCompilerExtensionPhase.AfterCompatibility))
			Add(tasks, "DingDong Compiler: Compatibility Extensions",
				RunCompatibilityExtensions, 0.01);
		Add(tasks, "DingDong Compiler: Natural Decoration", GenerateDecorations, 0.8);
		Add(tasks, "DingDong Compiler: Authored Spawn", GenerateAuthoredSpawn, 0.4);
		if (ModContent.GetInstance<DingDongServerConfig>().GenerateAuthoredSettlements)
			Add(tasks, "DingDong Compiler: Authored Residences", GenerateResidences, 1.0);
		Add(tasks, "DingDong Compiler: Finalize", FinalizeWorld, 0.5);
		Add(tasks, "DingDong Compiler: Receipt", CompleteReceipt, 0.01);
		for (int index = 0; index < tasks.Count; index++)
			tasks[index] = ObservePass(tasks[index]);
		totalWeight = 0;
		foreach (GenPass pass in tasks) totalWeight += pass.Weight;
		Mod.Logger.Info("DINGDONG_WORLD_COMPILER_TASKS|verdict=ARMED" +
			$"|passes={tasks.Count}|compatibility={compatibility.Count}" +
			$"|providers={compatibilityReceipt}|vanillaPasses=0");
	}

	private static void Add(List<GenPass> tasks, string name,
		WorldGenLegacyMethod method, double weight) =>
		tasks.Add(new PassLegacy(name, method, weight));

	private static GenPass WrapCompatibility(GenPass pass, int index) =>
		new PassLegacy(pass.Name, (progress, configuration) =>
		{
			pass.Apply(progress, configuration);
			if (pass.Name == "CursedCoffinArena")
				WorldCompilerProgressionChests.PlacePyramidsAfterArena(Plan, Receipt.Progression);
			Receipt.Compatibility.MarkCompleted(index, pass.Name);
		}, pass.Weight);

	private void Block(List<GenPass> tasks, ref double totalWeight, string reason)
	{
		tasks.Clear();
		tasks.Add(ObservePass(new PassLegacy("DingDong Compiler: Blocked", (_, _) =>
			throw new InvalidOperationException($"World compiler withheld: {reason}."), 1)));
		totalWeight = 1;
		Mod.Logger.Error($"DINGDONG_WORLD_COMPILER_TASKS|verdict=WITHHELD" +
			$"|reason={reason}|mutated=0");
	}
}
